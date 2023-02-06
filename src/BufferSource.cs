using System.Text;

namespace RizzziGit;

internal class BufferSource
{
  public BufferSource(byte[] data, int start, int end, bool copyOnWrite)
  {
    if (
      (start < 0) ||
      (data.Length < end)
    )
    {
      throw new IndexOutOfRangeException();
    }

    Data = data;
    Start = start;
    End = end;
    CopyOnWrite = copyOnWrite;
  }

  public BufferSource(byte[] data, int start, int end) : this(data, start, end, false) { }
  public BufferSource(byte[] data) : this(data, 0, data.Length) { }

  private byte[] Data;
  private int Start;
  private int End;
  private bool CopyOnWrite;
  public int Length { get => End - Start; }
  public int RealLength { get => Data.Length; }
  public bool IsTrimmed { get => Length != Data.Length; }

  public bool IsSameData(BufferSource source)
  {
    return (this == source) || (Data == source.Data);
  }

  public void Trim()
  {
    if (!IsTrimmed)
    {
      return;
    }

    byte[] newData = new byte[Length];

    System.Buffer.BlockCopy(Data, Start, newData, 0, Length);
    Start = 0;
    End = (Data = newData).Length;
  }

  public byte this[int index]
  {
    get
    {
      if (Length <= index)
      {
        throw new IndexOutOfRangeException();
      }

      return Data[Start + index];
    }

    set
    {
      if (Length <= index)
      {
        throw new IndexOutOfRangeException();
      }

      if (CopyOnWrite)
      {
        CopyOnWrite = false;
        System.Buffer.BlockCopy(Data, 0, Data = new byte[Data.Length], 0, Data.Length);
      }

      Data[Start + index] = value;
    }
  }

  public BufferSource Slice(int start, int end, bool copyOnWrite = true)
  {
    if (
      (start < 0) ||
      (end > Length)
    )
    {
      throw new IndexOutOfRangeException();
    }

    return new(Data, Start + start, Start + end, copyOnWrite);
  }

  public BufferSource Clone(bool copyOnWrite = true)
  {
    return new(Data, Start, End, copyOnWrite);
  }

  public byte[] ToByteArray()
  {
    byte[] output = new byte[Length];
    System.Buffer.BlockCopy(Data, Start, output, 0, output.Length);

    return output;
  }

  public new string ToString()
  {
    return Encoding.Default.GetString(ToByteArray());
  }
}
