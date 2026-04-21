public enum ToastStatus
{
	Info,
	Warning,
	Success,
	Error
}

public enum ToastPosition
{
	BottomRight,
	BottomLeft,
	TopLeft,
	TopRight
}

public struct Toast
{
	public string Text;
	public ToastStatus Status;
	public float Duration = 4;
	public ToastPosition Position = ToastPosition.BottomRight;
	public Toast() { }
}

public interface IToastEvent : ISceneEvent<IToastEvent>
{
	void Show( Toast toast ) { }
}
