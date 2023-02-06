using System.Text;

namespace RizzziGit;

public class Buffer
{

  public static Buffer Allocate(int length) {
    return new(new BufferSource[] { new(new byte[length], 0, length) });
  }

  public static Buffer FromByteArray(byte[] source, int start, int end)
  {
    return new(new BufferSource[] { new(source, start, end) });
  }

  public static Buffer FromByteArray(byte[] source) { return FromByteArray(source, 0, source.Length); }

  public static Buffer FromString(string source, int start, int end)
  {
    return FromByteArray(Encoding.Default.GetBytes(source.Substring(start, end - start)));
  }

  public static Buffer FromString(string source) { return FromString(source, 0, source.Length); }

  public static Buffer Concat(Buffer[] buffers)
  {
    List<BufferSource> sources = new();

    foreach (Buffer buffer in buffers)
    {
      sources.AddRange(buffer.Sources);
    }

    return new Buffer(sources.ToArray());
  }

  private Buffer(BufferSource[] sources)
  {
    Sources = sources;
  }

  private BufferSource[] Sources;
  public int Length
  {
    get
    {
      int length = 0;

      foreach (BufferSource source in Sources)
      {
        length += source.Length;
      }

      return length;
    }
  }

  public int RealLength
  {
    get
    {
      List<BufferSource> skipList = new();
      int realLength = 0;

      foreach (BufferSource source in Sources)
      {
        bool proceed = false;
        foreach (BufferSource skipSource in skipList)
        {
          if (skipSource.IsSameData(source))
          {
            proceed = true;
            break;
          }
        }

        if (proceed)
        {
          continue;
        }

        skipList.Add(source);
        realLength += source.RealLength;
      }

      return realLength;
    }
  }

  public bool IsFragmented
  {
    get
    {
      if (Sources.Length != 1)
      {
        return false;
      }

      foreach (BufferSource source in Sources)
      {
        if (!source.IsTrimmed)
        {
          return false;
        }
      }

      return true;
    }
  }

  public void Defragment()
  {
    Sources = new BufferSource[] { new(ToByteArray(), 0, Length) };
  }

  private void ResolveCoords(int index, out int x, out int y)
  {
    if (Length <= index)
    {
      throw new IndexOutOfRangeException();
    }

    x = index;
    for (y = 0; y < Sources.Length; y++)
    {
      BufferSource source = Sources[y];
      if (x < source.Length)
      {
        break;
      }

      x -= source.Length;
    }
  }

  public byte this[int index]
  {
    get
    {
      ResolveCoords(index, out int x, out int y);
      return Sources[y][x];
    }
    set
    {
      ResolveCoords(index, out int x, out int y);
      Sources[y][x] = value;
    }
  }

  public byte[] ToByteArray()
  {
    byte[] output = new byte[Length];

    int written = 0;
    foreach (BufferSource source in Sources)
    {
      System.Buffer.BlockCopy(source.ToByteArray(), 0, output, written, source.Length);
      written += source.Length;
    };

    return output;
  }

  public new string ToString()
  {
    return Encoding.Default.GetString(ToByteArray());
  }

  public Buffer Slice(int start, int end)
  {
    if (
      (end > Length) ||
      (end < start) ||
      (start < 0)
    )
    {
      throw new IndexOutOfRangeException();
    }
    else if (Length == 0)
    {
      return new(new BufferSource[] { });
    }
    else if (Sources.Length == 1)
    {
      return new(new BufferSource[] { Sources[0].Slice(start, end) });
    }

    List<BufferSource> output = new();
    int offset = start;
    int length = end - start;

    for (int index = 0; index < Sources.Length; index++)
    {
      BufferSource source = Sources[index];

      if (offset > 0)
      {
        if (offset >= source.Length)
        {
          offset -= source.Length;
          continue;
        }
        else
        {
          source = source.Slice(offset, source.Length);
          offset = 0;
        }
      }

      if (length > 0)
      {
        if (length < source.Length)
        {
          source = source.Slice(0, length);
          length = 0;
        }
        else
        {
          length -= source.Length;
        }

        output.Add(source);
      }
      else
      {
        break;
      }
    }

    return new(output.ToArray());
  }

  public Buffer PadEnd(int length)
  {
    if (Length < length)
    {
      return this;
    }

    return Concat(new Buffer[] { this, Buffer.Allocate(Length - length) });
  }

  public Buffer PadStart(int length)
  {
    if (Length >= length)
    {
      return this;
    }

    return Concat(new Buffer[] { Buffer.Allocate(Length - length), this });
  }

  public Buffer Clone()
  {
    return Buffer.FromByteArray(ToByteArray());
  }

  public bool Equals(Buffer another)
  {
    return (
      (this == another) ||
      (
        (Length == another.Length) &&
        (ToByteArray().SequenceEqual(another.ToByteArray()))
      )
    );
  }

  public Buffer Repeat(int count)
  {
    Buffer[] buffers = new Buffer[count];
    for (int index = 0; index < count; index++)
    {
      buffers[index] = this;
    }

    return Concat(buffers);
  }
}
