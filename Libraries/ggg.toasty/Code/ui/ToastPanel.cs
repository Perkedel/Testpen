using Sandbox.UI;
using Sandbox.UI.Construct;

public class ToastPanel : Panel
{
	public ToastPanel( Toast toast )
	{
		AddClass( toast.Status.ToString() );
		AddClass( toast.Position.ToString() );

		Add.Icon( toast.Status switch
		{
			ToastStatus.Info => "info",
			ToastStatus.Warning => "warning_amber",
			ToastStatus.Success => "check_circle_outline",
			ToastStatus.Error => "error_outline",
			_ => "",
		} );

		Add.Label( toast.Text, "text" );

		AddEventListener( "onclick", ( PanelEvent _ ) => Delete() );

		Invoke( toast.Duration, () => Delete() );
	}

	protected override int BuildHash() => HashCode.Combine( 1 );
}
