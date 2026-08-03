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
	[Header( "Link another heart organs wirelessly!" )]
	[Property, Feature( "Properties" ), Group( "Linking" )] public HeartOrgan[] OtherHearts { get; set; } // Useful for Linked List MultiHearts, or Quantumly Entangled Twins.
	[Header( "Adjust Funny effect chances here" )]
	[Property, Feature( "Properties" ), Group("Variance"),Advanced] public bool DiastoleReturnDeviates { get; set; } = false; // Is Diastole time deviates?
	[Property, Feature( "Properties" ), Group("Variance"),Advanced] public float DiastoleReturnDeviation { get; set; } = .3f; // How much Return Time deviates Plus minus from Decide Return?
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float SkipChancePercent { get; set; } = 0f; // Chance of unexpected extended silence Period Time
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float SkipSetTime { get; set; } = 2f; // and for how long it set instead
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float SkipSetTimeMin { get; set; } = 2f; // Minimum for random decide skip set time
	[Property, Feature( "Properties" ), Group( "Hiccup" ),Advanced] public float SkipSetTimeMax { get; set; } = 3f; // & max
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PVCSetTime { get; set; } = .1f; // and for how long it beats too early per previous PVC session
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PVCSetTimeMin { get; set; } = .1f; // Minimum for random decide to beats too early
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PVCSetTimeMax { get; set; } = .3f; // & max
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PostPVCSetTime { get; set; } = .1f; // and for how long it diastole too early after that skip
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PostPVCSetTimeMin { get; set; } = .1f; // Minimum for random decide to diastole too early time
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PostPVCSetTimeMax { get; set; } = .3f; // & max
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public float PVCChancePercent { get; set; } = 0f; // Chance of PVC
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public int PVCCountPerSessionMin { get; set; } = 2; // and how many PVC typically
	[Property, Feature( "Properties" ), Group("Hiccup"),Advanced] public int PVCCountPerSessionMax { get; set; } = 2; // and max

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
	// [Property, Feature( "Extra Debugs" ), Group( "Mathematics" ), ReadOnly] public float PVCPeriodT { get; set; } = 5f; // for PVC event
	[Property, Feature( "Extra Debugs" ), Group( "Mathematics" ), ReadOnly] public RealTimeUntil RemainPeriodRTU { get; set; } // s&box Realtime Untils! the Systole
	[Property, Feature( "Extra Debugs" ), Group( "Mathematics" ), ReadOnly] public RealTimeUntil StartReturnRTU { get; set; } // and Diastole

	[Header("Here's the Core of it!")]
	[Property, Feature("Extra Debugs"), Group("Core")] public bool Lub { get; set; } = false; // Heart Systole
	[Property, Feature( "Extra Debugs" ), Group( "Core" ), ReadOnly] int StateIndex { get; set; } = 0; // Heart organ state
	/*
	- 0
	- 1
	- 2
	- 3
	- 10 PVC Lub
	- 11 PVC Dub
	- 12 Awkward stun / Skip; hmmm use glitched out unexpected increased period time
	*/
	[Property, Feature("Extra Debugs"), Group("Core")] bool isBeating { get; set; } = true; // Heart is alive
	[Property, Feature( "Extra Debugs" ), Group( "Info" ), ReadOnly] string ToggleSay { get; set; } = ""; // Heart text info

	[Header( "Also Funny effect queue lines" )]
	// [Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public uint PrematureVentricularContractions { get; set; } = 0; // Heart had to PVC, pop number
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public uint SkipQueue { get; set; } = 0; // Heart had to skip, pop number
	// var DoesPVC ? -> the heart indeed is dadug dadug, not just a pause
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" )] public bool SkipInduced { get; set; } = false; // Manually induce skip
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" )] public bool WithPVC { get; set; } = false; // (argument for Skip) that is dadug-dadug
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ),ReadOnly] public bool GoingToSkip { get; set; } = false; // buffer for next beat will skip
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public bool SkipActive { get; internal set; } = false; // Heart is skipping, which is a
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public bool SkipDone { get; internal set; } = false; // Heart is skipping, which is a
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public bool IsPVC { get; internal set; } = false; // PVC?, which is Dadug-dadug? false = just a pause
	// [Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public bool PVCActive { get; internal set; } = false; // in PVC mode
	// [Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public bool PVCDone { get; internal set; } = false; // is it done yet?
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public int PVCNumbersInThisSession { get; set; } = 2; // How many PVC left now
	[Property, Feature( "Extra Debugs" ), Group( "Hiccups" ), ReadOnly] public float DiceRollSkip { get; set; } = 0f; // Roll the dice

	[Property, Feature("Scripting"), Group("Cookie Clicker")] public Doo OnLub{ get; set; }
	[Property, Feature( "Scripting" ), Group( "Rudimentary" )] public Ecghud Ecg { get; set; }

	public float NextSingle(float min = 0f, float max = 0f)
	{
		// https://gist.github.com/MachineCharmer/941949
		// http://stackoverflow.com/questions/1064901/random-number-between-2-double-numbers/1064907#1064907
		return Game.Random.NextSingle() * (max-min) + min;
	}

	public void HandoverEcg( Ecghud itThis )
	{
		Ecg = itThis;
	}

	protected void OnLubFunc()
	{
		if ( Ecg.IsValid() )
		{
			Ecg.CookieClick();
		}
	}

	// protected void DecideLubTime()
	// {

	// }

	public void InduceSkip(bool withPVC = false)
	{
		WithPVC = withPVC;
		GoingToSkip = true;
		// SkipInduced = true;
	}

	protected void DecideReturnTime( float forWhathowMuch = 0 )
	{
		if ( forWhathowMuch <= 0 )
		{
			// you are dead, not a big surprise.
			ToggleSay = "X_X Eik Serkat!";
			ReturnTime = 0f ;
		}
		else if ( forWhathowMuch >= 1 && forWhathowMuch < 20 )
		{
			ToggleSay = "...";
			ReturnTime = .75f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 20 && forWhathowMuch < 50 )
		{
			ToggleSay = "Looooooooww... heeeaarrt raaaaaate...";
			ReturnTime = .5f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 50 && forWhathowMuch < 70 )
		{
			// ToggleSay = "Sleepie";
			if ( forWhathowMuch == 69 ) ToggleSay = "nice";
			else if ( forWhathowMuch == 67 ) ToggleSay = "bruh";
			else ToggleSay = "Sleepie";
			ReturnTime = .3f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 70 && forWhathowMuch < 90 )
		{
			ToggleSay = "Heartbeat";
			ReturnTime = .25f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 90 && forWhathowMuch < 100 )
		{
			ToggleSay = "Accelerated";
			ReturnTime = .20f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 100 && forWhathowMuch < 150 )
		{
			ToggleSay = "FASS";
			ReturnTime = .15f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 150 && forWhathowMuch < 200 )
		{
			ToggleSay = "VERY FASS";
			ReturnTime = .1f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0);
		}
		else if ( forWhathowMuch >= 200 && forWhathowMuch < 300 )
		{
			ToggleSay = "TOO FASS";
			ReturnTime = .05f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0) * .01f;
		}
		else if ( forWhathowMuch >= 300 && forWhathowMuch < 400 )
		{
			ToggleSay = "EXTREMELY FASS";
			ReturnTime = .025f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0) * .01f;
		}
		else if ( forWhathowMuch >= 400 )
		{
			ToggleSay = "OH PECK!!! FIBRILATION GOING ON!!! OH NO!!!";
			ReturnTime = .001f + (DiastoleReturnDeviates? NextSingle(-DiastoleReturnDeviation,DiastoleReturnDeviation) : 0) * .001f;
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
		if ( _Hr >= CriticalMaxRate )
		{
			HeartRate = CriticalMaxRate;
			_Hr = CriticalMaxRate;
		}
		if(!SkipActive || SkipDone)
		{
			DecideReturnTime( _Hr );
			Hertz = _Hr > 0 ? _Hr / 60 : 1;
			PeriodT = Hertz > 0 ? 1 / Hertz : 1;
		} else
		{
			Hertz = _Hr > 0 ? _Hr / 60 : 1;
			// if(SkipActive && SkipDone)
			// {
			// 	SkipActive = false;
			// 	SkipDone = false;
			// }
		}
	}

	protected void AsyncHeartInteruptUpdate( float Delta )
	{
		// Now if there is pending hiccups used, here procedure!


		// if ( Lub )
		// {

		// }

		// if(PVCDone)
		// {
		// 	// Pop the queue!
		// 	if ( PrematureVentricularContractions > 0 ) PrematureVentricularContractions--; // only pop when is more than 0
		// 	// otherwise you'd roll over to 2^32 whatever this is.
		// 	if ( PrematureVentricularContractions < 0 ) PrematureVentricularContractions = 0; // no I don't think that's how uint work..
		// }
	}

	protected void DazedHeartbeatSubsystem( float Delta)
	{
		if ( ((SkipChancePercent > 0f && DiceRollSkip < SkipChancePercent) && !SkipActive) || GoingToSkip )
		{
			// Brainstormed by Joel. that's it. Only a human.
			SkipActive = true;
			SkipInduced = false;
			GoingToSkip = false;
			IsPVC = WithPVC;

			// decide skip time
			// SkipSetTime = Game.Random.Next(SkipSetTimeMin,SkipSetTimeMax);
			// SkipSetTime = Game.Random.NextSingle();
			// bro
			// ~~https://medium.com/analytics-vidhya/random-floats-in-any-range-9b40d30b637b~~
			// https://gist.github.com/MachineCharmer/941949
			// http://stackoverflow.com/questions/1064901/random-number-between-2-double-numbers/1064907#1064907
			// why the fuck no range?!?!?
			SkipSetTime = Game.Random.NextSingle() * (SkipSetTimeMax - SkipSetTimeMin) + SkipSetTimeMin;
			Log.Info( $"Skip Set time is {SkipSetTime} " );
			PVCSetTime = Game.Random.NextSingle() * (PVCSetTimeMax - PVCSetTimeMin) + PVCSetTimeMin;
			PostPVCSetTime = Game.Random.NextSingle() * (PostPVCSetTimeMax - PostPVCSetTimeMin) + PostPVCSetTimeMin;

			// _capT += 12f;
			// _capT = PVCSetTime;
			PeriodT = IsPVC? PVCSetTime : SkipSetTime;
			RemainPeriodT = IsPVC ? PVCSetTime : SkipSetTime;
			RemainPeriodRTU = IsPVC ? PVCSetTime : SkipSetTime;
			ReturnTime = IsPVC? PostPVCSetTime : ReturnTime;
			// StartReturnTime = PostPVCSetTime;

			// if ( SkipQueue > 0 ) SkipQueue--;
			// if ( SkipQueue < 0 ) SkipQueue = 0;
			PVCNumbersInThisSession = Game.Random.Next( PVCCountPerSessionMin, PVCCountPerSessionMax );
		}
		else
		{
			if ( SkipActive )
			{

			}
		}
		// return void;
	}

	protected void AsyncHeartbeatUpdate( float Delta )
	{
		float _capT = Math.Min( RemainPeriodT, 1 / (HeartRate / 60) ); // cap the perioding!

		// but if heart had to skip a beat
		DazedHeartbeatSubsystem( Delta );
		if(SkipActive)
		{
			_capT = RemainPeriodT;
		}

		if ( RemainPeriodT > _capT )
		{
			RemainPeriodT = _capT;
			RemainPeriodRTU = _capT;
			return;
		}
		if (RemainPeriodT < 0)
		{
			RemainPeriodT = 0;
			RemainPeriodRTU = 0;
			return;
		}
		RemainPeriodT -= Delta;
		RemainPeriodTMillisec = RemainPeriodT * 1000f;
		if ( RemainPeriodTMillisec <= 0 )
		{
			// But before systole, check if there is Hiccup on queue
			// if(PrematureVentricularContractions > 0)
			// {
			// 	// interupt & go to this special procedure instead!
			// 	// return
			// }

			// Heart Systole
			StateIndex = 1;
			RemainPeriodT = PeriodT;
			RemainPeriodRTU = PeriodT;
			RemainPeriodTMillisec = RemainPeriodT * 1000f;
			Lub = true;
			// DONE: ECG & sound
			if ( EnableSound )
			{
				if ( SystoleSound.IsValid() ) Sound.Play( SystoleSound, WorldPosition );
			}
			RunDoo( OnLub );
			OnLubFunc();
			if(SkipActive)
			{
				if ( IsPVC )
					// check count phase
					if(PVCNumbersInThisSession > 0)
					{
						// ongoing count
						PVCSetTime = Game.Random.NextSingle() * (PVCSetTimeMax - PVCSetTimeMin) + PVCSetTimeMin;
						PostPVCSetTime = Game.Random.NextSingle() * (PostPVCSetTimeMax - PostPVCSetTimeMin) + PostPVCSetTimeMin;

						// _capT = PVCSetTime;
						PeriodT = PVCSetTime;
						// RemainPeriodT = PVCSetTime;
						ReturnTime = PostPVCSetTime;

						if ( PVCNumbersInThisSession > 0 ) PVCNumbersInThisSession--;
						if ( PVCNumbersInThisSession < 0 ) PVCNumbersInThisSession = 0;
					} else
					{
						// this is the last one!
						// SkipSetTime = Game.Random.NextSingle() * (SkipSetTimeMax - SkipSetTimeMin) + SkipSetTimeMin;
						// _capT = SkipSetTime;
						PeriodT = SkipSetTime;
						// RemainPeriodT = SkipSetTime;
						// so it pauses really
						// SkipActive = false;
						SkipDone = true;
					}

				else
				{
					// Just a pause then? okay let's call it immediately.
					// _capT = SkipSetTime;
					PeriodT = SkipSetTime;
					// RemainPeriodT = SkipSetTime;
					SkipDone = true;
				}
			}
		}
		else
		{
			// DONE: ECG
		}

		if ( Lub )
		{
			StartReturnTime -= Delta;
			StartReturnTimeMillisec = StartReturnTime * 1000f;
			if ( StartReturnTimeMillisec <= 0 )
			{
				// Heart Diastole
				StateIndex = 0;
				StartReturnTime = ReturnTime;
				StartReturnRTU = ReturnTime; // NEW
				StartReturnTimeMillisec = StartReturnTime * 1000f;
				Lub = false;
				// DONE: ECG & sound
				if ( EnableSound )
				{
					if ( DiastoleSound.IsValid() ) Sound.Play( DiastoleSound, WorldPosition );
				}
				if(SkipActive)
				{
					if(IsPVC && PVCNumbersInThisSession == 1)
					{
						// the last PVC before "null" terminator (0), try go to pause
						PeriodT = SkipSetTime;
						// RemainPeriodT = PeriodT;
						// RemainPeriodRTU = PeriodT;
					}
					if(SkipActive && SkipDone)
					{
						PeriodT = SkipSetTime;
						RemainPeriodT = PeriodT;
						RemainPeriodRTU = PeriodT;
						SkipActive = false;
						SkipDone = false;
						// heart got it together again
						// ResyncHeartUpdate( Delta );
					}
				} else
				{
					// check if heart will skip
					// if ( (( SkipChancePercent > 0f && DiceRollSkip < SkipChancePercent ) && !SkipActive) || SkipInduced )
					// {
					// 	DazedHeartbeatSubsystem( Delta );
					// }
					if ( GoingToSkip ) SkipActive = true;
					// Refresh Remain Period T, partial resync
					// RemainPeriodT = PeriodT;
					// dice roll for next beat
					DiceRollSkip = Game.Random.NextSingle() * 100f;
				}
			}
			else
			{
				// DONE: ECG
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
				StartReturnRTU = ReturnTime;
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

	protected override void OnStart()
	{
		// Check the Doo

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
