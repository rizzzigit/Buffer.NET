using System.Text;

namespace RizzziGit;

public class Buffer
{
  public static explicit operator Buffer(byte[] input)
  {
    return FromByteArray(input);
  }

  public static explicit operator byte[](Buffer input)
  {
    return input.ToByteArray();
  }

  public static bool operator ==(Buffer? left, object? right)
  {
    return left?.GetHashCode() == right?.GetHashCode();
  }

  public static bool operator !=(Buffer? left, object? right)
  {
    return !(left == right);
  }

  public static Buffer Random(int length)
  {
    byte[] data = new byte[length];
    System.Random.Shared.NextBytes(data);

    return new(new BufferSource[] { new(data, 0, length, false) });
  }

  public static Buffer Allocate(int length)
  {
    if (length == 0)
    {
      return new(new BufferSource[] { });
    }

    return new(new BufferSource[] { new(new byte[length], 0, length, false) });
  }

  public static Buffer FromByteArray(byte[] source, int start, int end, bool copyOnWrite = true)
  {
    return new(new BufferSource[] { new(source, start, end, copyOnWrite) });
  }

  public static Buffer FromByteArray(byte[] source, bool copyOnWrite = true) { return FromByteArray(source, 0, source.Length, copyOnWrite); }

  public static Buffer FromString(string source, int start, int end)
  {
    byte[] byteSource = Encoding.Default.GetBytes(source.Substring(start, end - start));
    return FromByteArray(byteSource, 0, byteSource.Length, false);
  }

  public static Buffer FromString(string source) { return FromString(source, 0, source.Length); }

  public static Buffer FromHex(string source)
  {
    byte[] bytes = new byte[source.Length / 2];
    for (int index = 0; index < source.Length; index += 2)
    {
      bytes[index / 2] = Convert.ToByte(source.Substring(index, 2), 16);
    }

    return Buffer.FromByteArray(bytes);
  }

  public static Buffer FromInt32(int integer)
  {
    byte[] bytes = BitConverter.GetBytes(integer);
    if (BitConverter.IsLittleEndian)
    {
      Array.Reverse(bytes);
    }

    Buffer output = FromByteArray(bytes);
    int offset = 0;
    for (; offset < output.Length && (output[offset] == 0); offset++) ;

    return (offset == 4) ? Buffer.Allocate(1) : output.Slice(offset, output.Length);
  }

  public static Buffer FromInt16(short integer)
  {
    byte[] bytes = BitConverter.GetBytes(integer);
    if (BitConverter.IsLittleEndian)
    {
      Array.Reverse(bytes);
    }

    Buffer output = FromByteArray(bytes);
    int offset = 0;
    for (; offset < output.Length && (output[offset] == 0); offset++) ;

    return (offset == 2) ? Buffer.Allocate(1) : output.Slice(offset, output.Length);
  }

  public static Buffer Concat(Buffer[] buffers)
  {
    List<BufferSource> sources = new();

    foreach (Buffer buffer in buffers)
    {
      if (buffer.Length == 0)
      {
        continue;
      }

      foreach (BufferSource bufferSource in buffer.Sources)
      {
        sources.Add(bufferSource.Clone());
      }
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
    Sources = new BufferSource[] { new(ToByteArray()) };
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

      if (HashCodeCache != null)
      {
        HashCodeCache = null;
      }
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

  public string ToHex()
  {
    return BitConverter.ToString(ToByteArray()).Replace("-", "");
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
          source = source.Clone();
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

  public int Read(byte[] buffer, int position, int length)
  {
    Buffer output = Slice(position, position + length > Length ? Length : position + length);
    System.Buffer.BlockCopy(output.ToByteArray(), 0, buffer, 0, output.Length);
    return output.Length;
  }

  public int Write(byte[] buffer, int sourceOffset, int destinationOffset, int length)
  {
    try
    {
      int bytesWritten = 0;
      foreach (BufferSource source in Sources)
      {
        if (destinationOffset >= source.Length)
        {
          destinationOffset -= source.Length;
          continue;
        }

        if (source.Length < (destinationOffset + length))
        {
          source.Write(buffer, sourceOffset, destinationOffset, source.Length - destinationOffset);
          int written = source.Length - destinationOffset;

          length -= written;
          sourceOffset += written;
          bytesWritten += written;

          destinationOffset = 0;
        }
        else
        {
          source.Write(buffer, sourceOffset, destinationOffset, length);

          sourceOffset += length;
          bytesWritten += length;
          length = 0;

          destinationOffset = 0;
          break;
        }
      }

      return bytesWritten;
    }
    finally
    {
      if (HashCodeCache != null)
      {
        HashCodeCache = null;
      }
    }
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
    BufferSource[] sources = new BufferSource[Sources.Length];
    for (int index = 0; index < Sources.Length; index++)
    {
      sources[index] = Sources[index].Clone(true);
    }

    return new(sources);
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

  public override bool Equals(object? obj)
  {
    Buffer? objCastd = obj as Buffer;
    if (objCastd == null)
    {
      return false;
    }

    return this.Equals((Buffer)objCastd);
  }

  private int? HashCodeCache;
  public override int GetHashCode()
  {
    if (HashCodeCache != null)
    {
      return (int)HashCodeCache;
    }

    HashCode code = new();
    code.AddBytes(ToByteArray());
    return (int)(HashCodeCache = code.ToHashCode());
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

  public int ToInt32()
  {
    if (Length > 4)
    {
      throw new Exception("Length must be less than or equal to 4.");
    }

    byte[] bytes = PadStart(4).ToByteArray();
    if (BitConverter.IsLittleEndian)
    {
      Array.Reverse(bytes);
    }

    return BitConverter.ToInt32(bytes);
  }

  public short ToInt16()
  {
    if (Length > 2)
    {
      throw new Exception("Length must be less than or equal to 2.");
    }

    return (short)ToInt32();
  }
}
