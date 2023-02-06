using System.Text;

namespace RizzziGit;

internal class BufferSource
{
  public BufferSource(byte[] data, int start, int end)
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
  }

  private byte[] Data;
  private int Start;
  private int End;
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

      Data[Start + index] = value;
    }
  }

  public BufferSource Slice(int start, int end)
  {
    if (
      (start < 0) ||
      (end > Length)
    )
    {
      throw new IndexOutOfRangeException();
    }

    return new(Data, Start + start, Start + end);
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
