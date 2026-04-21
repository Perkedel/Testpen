using System.Collections.Generic;
using Sandbox.UI;

public class ToastsDisplay : PanelComponent, IToastEvent
{
	private readonly Dictionary<ToastPosition, Panel> Positions = new();
	protected override void OnStart()
	{
		base.OnStart();

		Positions.Add( ToastPosition.TopLeft, Panel.Add.Panel( "toastcontainer TopLeft" ) );
		Positions.Add( ToastPosition.TopRight, Panel.Add.Panel( "toastcontainer TopRight" ) );
		Positions.Add( ToastPosition.BottomRight, Panel.Add.Panel( "toastcontainer BottomRight" ) );
		Positions.Add( ToastPosition.BottomLeft, Panel.Add.Panel( "toastcontainer BottomLeft" ) );
	}
	void IToastEvent.Show( Toast toast )
	{
		Show( toast );
	}

	public void Show( Toast toast )
	{
		Positions[toast.Position].AddChild( new ToastPanel( toast ) );
	}

	[ConCmd( "toasts_test" )]
	public static void TestToasts()
	{
		var td = Game.ActiveScene.Camera.GameObject.GetComponent<ToastsDisplay>();

		td.Show( new Toast
		{
			Status = ToastStatus.Info,
			Text = "Content should be short",
			Position = ToastPosition.TopLeft,
		} );

		td.Show( new Toast
		{
			Status = ToastStatus.Warning,
			Text = "Something interesting happened",
			Duration = 10,
			Position = ToastPosition.TopRight,
		} );

		td.Show( new Toast
		{
			Status = ToastStatus.Error,
			Text = "Errors",
			Position = ToastPosition.BottomRight,
		} );

		td.Show( new Toast
		{
			Status = ToastStatus.Error,
			Text = "Cool right!",
			Position = ToastPosition.BottomLeft,
		} );
	}


	[ConCmd( "toasts_clear" )]
	public static void ClearToasts()
	{
		var td = Game.ActiveScene.Camera.GameObject.GetComponent<ToastsDisplay>();
		foreach ( var p in td.Panel.Children.ToList() )
			p.Delete();
	}
}
