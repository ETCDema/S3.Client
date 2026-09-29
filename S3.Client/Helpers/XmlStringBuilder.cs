using LinkDotNet.StringBuilder;

namespace S3.Client.Helpers;

internal ref struct XmlStringBuilder(bool pretty): IDisposable
{
    private ValueStringBuilder _sb	= new(1024);
    private int _level = 0;

    public void WriteTagStart(string tag)
    {
        if (_sb.Length > 0 && pretty)
        {
            _sb.Append('\n');
            Indent();
        }

        _sb.Append('<');
        _sb.Append(tag);
        _sb.Append('>');

        _level++;
    }

    public void WriteTag(string tag, ReadOnlySpan<char> value)
    {
        if (_sb.Length > 0 && pretty)
        {
            _sb.Append('\n');
            Indent();
        }

        _sb.Append('<');
        _sb.Append(tag);
        _sb.Append('>');

        _sb.Append(value);

        _sb.Append("</");
        _sb.Append(tag);
        _sb.Append('>');
    }

    public void WriteTagEnd(string tag)
    {
        _level--;

        if (_sb.Length > 0 && pretty)
        {
            _sb.Append('\n');
            Indent();
        }

        _sb.Append("</");
        _sb.Append(tag);
        _sb.Append('>');
    }

    public void Indent()
    {
        if (!pretty) return;

        for (int i = 0; i < _level; i++)
        {
            _sb.Append("  ");
        }
    }

    public override string ToString()
    {
        return _sb.ToString();
    }

	public void Dispose()
	{
		_sb.Dispose();
	}
}
