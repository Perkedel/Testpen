/*
(c) Perkedel Technologies
GNU GPL v3
*/

namespace Sandbox;

public sealed class HeartOrgan : Component
{
	/*
	Let's port this from our Godot prototype.
	SAUCEs:
	- https://github.com/Perkedel/HexagonEngine/blob/4-transitioning/GameDVDCardtridge/DetakJantungProsotipe/DetakJantungProsotipe.gd
	- https://github.com/Perkedel/HeartbeatOpenScript-Unity/blob/master/Assets/HeartbeatOpenScript.cs
	- https://github.com/godotengine/godot/issues/15895#issuecomment-359185065 OGG sound loop
	- https://docs.godotengine.org/en/stable/tutorials/gui/bbcode_in_richtextlabel.html BBCode

	Jesus Christ! We made that monolithicly! let's not make the same mistake!
	Handmade only!
	*/
	[Property, Feature("Properties")] public float HeartRate { get; set; } = 70f; // Star of the show!
	[Property, Feature("Properties")] public float IdealRate { get; set; } = 70f; // Homo sapiens heart rate ideally is
	[Property, Feature( "Properties" )] public float MaximumRate { get; set; } = 500f; // Maximum BPM cap from normal doing

	//[Property, Feature("Properties")] public Ecghud ECGMonitor { get; set; } // Insert ECG here!

	[Property, Feature( "Sounds" )] public bool EnableSound = true;
	[Property, Feature( "Sounds" )] public SoundEvent SystoleSound { get; set; } = ResourceLibrary.Get<SoundEvent>( "sound/dot name/systole.sound" );
	[Property, Feature( "Sounds" )] public SoundEvent DiastoleSound { get; set; } = ResourceLibrary.Get<SoundEvent>( "sound/dot name/diastole.sound" );
	[Property, Feature( "Sounds" ), RequireComponent] public SoundPointComponent Speaker { get; set; }

	[Property, Feature( "Extra Debugs" ), Group( "Critical" ), ReadOnly] protected float CriticalMaxRate { get; set; } = 99999f; // Techical maxium rate from cheated doing

	[Header("Here's the Mathematics!")]
	[Property, Feature("Extra Debugs"), Group("Mathematics"), ReadOnly] float Hertz { get; set; } = 70f / 60f; // HeartRate / 60
	[Property, Feature("Extra Debugs"), Group("Mathematics"), ReadOnly] public float PeriodT { get; set; } = 1 / (70 / 60); // 1 / Hertz
	[Property, Feature("Extra Debugs"), Group("Running"), ReadOnly] public float RemainPeriodT { get; set; } = 1 / (70 / 60); // catch the PeriodT & reset to PeriodT
	[Property, Feature("Extra Debugs"), Group("Running Conversion"), ReadOnly] float RemainPeriodTMillisec { get; set; } = (1 / (70 / 60)) * 1000f; // RemainPeriodT * 1000
	[Property, Feature("Extra Debugs"), Group("Mathematics"), ReadOnly] public float ReturnTime { get; set; } = .25f; // When heart will diastole back?
	[Property, Feature("Extra Debugs"), Group("Running"), ReadOnly] public float StartReturnTime { get; set; } = .25f; // catch ReturnTime now & reset based ReturnTime
	[Property, Feature( "Extra Debugs" ), Group( "Running Conversion" ), ReadOnly] float StartReturnTimeMillisec { get; set; } = (.25f) * 1000f; // StartReturnTime * 1000

	[Header("Here's the Core of it!")]
	[Property, Feature("Extra Debugs"), Group("Core")] public bool Lub { get; set; } = false; // Heart Systole
	[Property, Feature("Extra Debugs"), Group("Core"), ReadOnly] int StateIndex { get; set; } = 0; // Heart organ state
	[Property, Feature("Extra Debugs"), Group("Core")] bool isBeating { get; set; } = true; // Heart is alive
	[Property, Feature("Extra Debugs"), Group("Info"), ReadOnly] string ToggleSay { get; set; } = ""; // Heart text info

	protected void DecideReturnTime( float forWhathowMuch = 0 )
	{
		if ( forWhathowMuch <= 0 )
		{
			// you are dead, not a big surprise.
			ToggleSay = "X_X Eik Serkat!";
			ReturnTime = 0f;
		}
		else if ( forWhathowMuch >= 1 && forWhathowMuch < 20 )
		{
			ToggleSay = "...";
			ReturnTime = .75f;
		}
		else if ( forWhathowMuch >= 20 && forWhathowMuch < 50 )
		{
			ToggleSay = "Looooooooww... heeeaarrt raaaaaate...";
			ReturnTime = .5f;
		}
		else if ( forWhathowMuch >= 50 && forWhathowMuch < 70 )
		{
			// ToggleSay = "Sleepie";
			if ( forWhathowMuch == 69 ) ToggleSay = "nice";
			else if ( forWhathowMuch == 67 ) ToggleSay = "bruh";
			else ToggleSay = "Sleepie";
			ReturnTime = .3f;
		}
		else if ( forWhathowMuch >= 70 && forWhathowMuch < 90 )
		{
			ToggleSay = "Heartbeat";
			ReturnTime = .25f;
		}
		else if ( forWhathowMuch >= 90 && forWhathowMuch < 100 )
		{
			ToggleSay = "Accelerated";
			ReturnTime = .20f;
		}
		else if ( forWhathowMuch >= 100 && forWhathowMuch < 150 )
		{
			ToggleSay = "FASS";
			ReturnTime = .15f;
		}
		else if ( forWhathowMuch >= 150 && forWhathowMuch < 200 )
		{
			ToggleSay = "VERY FASS";
			ReturnTime = .1f;
		}
		else if ( forWhathowMuch >= 200 && forWhathowMuch < 300 )
		{
			ToggleSay = "TOO FASS";
			ReturnTime = .05f;
		}
		else if ( forWhathowMuch >= 300 && forWhathowMuch < 400 )
		{
			ToggleSay = "EXTREMELY FASS";
			ReturnTime = .025f;
		}
		else if ( forWhathowMuch >= 400 )
		{
			ToggleSay = "OH PECK!!! FIBRILATION GOING ON!!! OH NO!!!";
			ReturnTime = .001f;
		}
		else
		{
			ToggleSay = "???";
		}
	}

	public void SetHeartRate( float intoValueOf = 70 )
	{
		HeartRate = intoValueOf;
		if ( HeartRate >= 1 )
		{
			isBeating = true;
		}
		else if ( HeartRate <= 0 )
		{
			Log.Info( "Stop Heartbeat!" );
			isBeating = false;
		}
		DecideReturnTime( intoValueOf );
		Log.Info( $"Set HeartRate into {HeartRate} BPM" );
		Hertz = HeartRate > 0 ? HeartRate / 60 : 1;
		PeriodT = Hertz > 0 ? 1 / Hertz : 1;
		ResyncHeartUpdate(Time.Delta);
	}

	public void AddHeartRate( float ByWhat = 1 )
	{
		SetHeartRate( HeartRate + ByWhat );
	}

	public void ResetHeartRate()
	{
		// Not to be confused with CPR & Defib!
		SetHeartRate( IdealRate );
	}

	public void ToggleInternalSound()
	{
		EnableSound = !EnableSound;
	}

	protected void ResyncHeartUpdate( float Delta )
	{
		float _Hr = HeartRate;
		if ( _Hr >= 1 ) isBeating = true;
		else isBeating = false;
		if ( _Hr < 0 )
		{
			HeartRate = 0;
			_Hr = 0;
		}
		if ( _Hr >= CriticalMaxRate)
		{
			HeartRate = CriticalMaxRate;
			_Hr = CriticalMaxRate;
		}
		DecideReturnTime( _Hr );
		Hertz = _Hr > 0 ? _Hr / 60 : 1;
		PeriodT = Hertz > 0 ? 1 / Hertz : 1;
	}

	protected void AsyncHeartbeatUpdate( float Delta )
	{
		float _capT = Math.Min( RemainPeriodT, 1 / (HeartRate / 60) ); // cap the perioding!
		if ( RemainPeriodT > _capT )
		{
			RemainPeriodT = _capT;
			return;
		}
		RemainPeriodT -= Delta;
		RemainPeriodTMillisec = RemainPeriodT * 1000f;
		if ( RemainPeriodTMillisec <= 0 )
		{
			StateIndex = 1;
			RemainPeriodT = PeriodT;
			RemainPeriodTMillisec = RemainPeriodT * 1000f;
			Lub = true;
			// TODO: ECG & sound
			if(EnableSound){
				if ( SystoleSound.IsValid() ) Sound.Play( SystoleSound, WorldPosition );
			}
		}
		else
		{
			// TODO: ECG
		}

		if ( Lub )
		{
			StartReturnTime -= Delta;
			StartReturnTimeMillisec = StartReturnTime * 1000f;
			if ( StartReturnTimeMillisec <= 0 )
			{
				StateIndex = 0;
				StartReturnTime = ReturnTime;
				StartReturnTimeMillisec = StartReturnTime * 1000f;
				Lub = false;
				// TODO: ECG & sound
				if(EnableSound)
				{
					if ( DiastoleSound.IsValid()) Sound.Play( DiastoleSound, WorldPosition );
				}
			}
			else
			{
				// TODO: ECG
			}
		}
	}

	protected void NoHeartbeatEikSerkat( float Delta )
	{
		RemainPeriodT = PeriodT;

		// Finish everything first!
		if ( Lub )
		{
			StartReturnTime -= Delta;
			StartReturnTimeMillisec = StartReturnTime * 1000f;
			if ( StartReturnTimeMillisec <= 0 )
			{
				StateIndex = 0;
				StartReturnTime = ReturnTime;
				StartReturnTimeMillisec = StartReturnTime * 1000f;
				Lub = false;
				// TODO: ECG & sound
			}
			else
			{
				// TODO: ECG
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( isBeating )
		{
			AsyncHeartbeatUpdate( Time.Delta );
		}
		else
		{
			NoHeartbeatEikSerkat( Time.Delta );
		}
		ResyncHeartUpdate( Time.Delta );

		var _a_ = Game.ActiveScene;
	}
}
