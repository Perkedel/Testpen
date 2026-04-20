using Sandbox;

public sealed class GameNotPad : Component
{
	[Property] public AnalogStickTest MovementTest { get; set; }
	[Property] public AnalogStickTest CameraTest { get; set; }
	[Property] public AnalogStickTest GyroTest { get; set; }
	[Property] public TrackballTest TrackballTest { get; set; }

	[Property] public SquishTest JumpTest { get; set; }
	[Property] public SquishTest CircleTest { get; set; }
	[Property] public SquishTest SquareTest { get; set; }
	[Property] public SquishTest TriangleTest { get; set; }

	[Property] public SquishTest DpadDownTest { get; set; }
	[Property] public SquishTest DpadRightTest { get; set; }
	[Property] public SquishTest DpadLeftTest { get; set; }
	[Property] public SquishTest DpadUpTest { get; set; }

	[Property] public SquishTest PrimaryAttackTest { get; set; }
	[Property] public SquishTest AltAttackTest { get; set; }

	[Property] public SquishTest LeftBumperTest { get; set; }
	[Property] public SquishTest RightBumperTest { get; set; }

	[Property] public SquishTest ShareButtonTest { get; set; }
	[Property] public SquishTest StartButtonTest { get; set; }
	[Property] public SquishTest SelectButtonTest { get; set; } // a.k.a Touchpad.
	// But including s&box all fucking utterly wrong, thancc alot SDL! You fixed it fucking late!
	// Sony too! why the fuck proprietary?!

	[Property] public SquishTest GuideButtonTest { get; set; }
	[Property] public SquishTest MuteButtonTest { get; set; }

	[Property] public SquishTest[] PaddleButtons { get; set; }

	protected override void OnAwake()
	{

	}

	protected override void OnStart()
	{
		// set image!
		if ( MovementTest.IsValid() )
		{
			MovementTest.expectedGlyphName = "Run";
		}
		if ( CameraTest.IsValid() )
		{
			CameraTest.expectedGlyphName = "View";
		}

		if ( JumpTest.IsValid() )
		{
			JumpTest.expectedGlyphName = "jump";
		}
		if ( CircleTest.IsValid() )
		{
			CircleTest.expectedGlyphName = "duck";
		}
		if ( SquareTest.IsValid() )
		{
			SquareTest.expectedGlyphName = "reload";
		}
		if ( TriangleTest.IsValid() )
		{
			TriangleTest.expectedGlyphName = "use";
		}

		if ( DpadDownTest.IsValid() )
		{
			DpadDownTest.expectedGlyphName = "slot3";
		}
		if ( DpadRightTest.IsValid() )
		{
			DpadRightTest.expectedGlyphName = "slot2";
		}
		if ( DpadLeftTest.IsValid() )
		{
			DpadLeftTest.expectedGlyphName = "slot1";
		}
		if ( DpadUpTest.IsValid() )
		{
			DpadUpTest.expectedGlyphName = "flashlight";
		}

		if ( PrimaryAttackTest.IsValid() )
		{
			PrimaryAttackTest.expectedGlyphName = "Attack1";
		}
		if ( AltAttackTest.IsValid() )
		{
			AltAttackTest.expectedGlyphName = "Attack2";
		}

		if ( LeftBumperTest.IsValid() )
		{
			LeftBumperTest.expectedGlyphName = "SlotPrev";
		}
		if ( RightBumperTest.IsValid() )
		{
			RightBumperTest.expectedGlyphName = "SlotNext";
		}

		if ( ShareButtonTest.IsValid() )
		{
			ShareButtonTest.expectedGlyphName = "GuideShare";
		}
		if ( StartButtonTest.IsValid() )
		{
			StartButtonTest.expectedGlyphName = "GuideStart";
		}
		if ( SelectButtonTest.IsValid() )
		{
			SelectButtonTest.expectedGlyphName = "GuideSelect";
		}

		if ( GuideButtonTest.IsValid() )
		{
			GuideButtonTest.expectedGlyphName = "GuideShare";
		}
		if ( MuteButtonTest.IsValid() )
		{
			MuteButtonTest.expectedGlyphName = "GuideShare";
		}

		for(int i = 0;i<=3; i++)
		{
			if(PaddleButtons[i].IsValid())
			{
				PaddleButtons[i].expectedGlyphName = String.Format( "paddleButton_%d", i );
			}
		}
	}

	protected override void OnUpdate()
	{
		// https://sbox.game/dev/doc/gameplay/input/controller-input

		if ( MovementTest.IsValid() )
		{
			//MovementTest.Transform.Position = new Vector3();
			MovementTest.SetAnalog( Input.AnalogMove );

			float beSet = Input.Down( "Run" ) ? 1f : 0f;
			MovementTest.SetSquish( beSet );
		}
		if ( CameraTest.IsValid() )
		{
			//CameraTest.SetAnalog( Input.AnalogLook.AsVector3());
			CameraTest.SetAnalog( new Vector2( -Input.GetAnalog( InputAnalog.RightStickY ), -Input.GetAnalog( InputAnalog.RightStickX ) ) );

			float beSet = Input.Down( "View" ) ? 1f : 0f;
			CameraTest.SetSquish( beSet );
		}
		if ( TrackballTest.IsValid() )
		{
			TrackballTest.SpinAnalog( Input.AnalogLook.ToRotation() );
			//TrackballTest.SetAnalog( Input.AnalogLook.ToRotation() );
		}

		if ( JumpTest.IsValid() )
		{
			float beSet = Input.Down( "Jump" ) ? 1f : 0f;
			JumpTest.SetAnalog( beSet );
		}
		if ( CircleTest.IsValid() )
		{
			float beSet = Input.Down( "duck" ) ? 1f : 0f;
			CircleTest.SetAnalog( beSet );
		}
		if ( SquareTest.IsValid() )
		{
			float beSet = Input.Down( "reload" ) ? 1f : 0f;
			SquareTest.SetAnalog( beSet );
		}
		if ( TriangleTest.IsValid() )
		{
			float beSet = Input.Down( "use" ) ? 1f : 0f;
			TriangleTest.SetAnalog( beSet );
		}

		if ( DpadDownTest.IsValid() )
		{
			float beSet = Input.Down( "slot3" ) ? 1f : 0f;
			DpadDownTest.SetAnalog( beSet );
		}
		if ( DpadRightTest.IsValid() )
		{
			float beSet = Input.Down( "slot2" ) ? 1f : 0f;
			DpadRightTest.SetAnalog( beSet );
		}
		if ( DpadLeftTest.IsValid() )
		{
			float beSet = Input.Down( "slot1" ) ? 1f : 0f;
			DpadLeftTest.SetAnalog( beSet );
		}
		if ( DpadUpTest.IsValid() )
		{
			float beSet = Input.Down( "flashlight" ) ? 1f : 0f;
			DpadUpTest.SetAnalog( beSet );
		}

		if ( ShareButtonTest.IsValid() )
		{
			float beSet = Input.Down( "GuideShare" ) ? 1f : 0f;
			ShareButtonTest.SetAnalog( beSet );
		}
		if ( StartButtonTest.IsValid() )
		{
			float beSet = Input.Down( "GuideStart" ) ? 1f : 0f;
			StartButtonTest.SetAnalog( beSet );
		}
		if ( SelectButtonTest.IsValid() )
		{
			float beSet = Input.Down( "GuideSelect" ) ? 1f : 0f;
			SelectButtonTest.SetAnalog( beSet );
		}

		if ( GuideButtonTest.IsValid() )
		{
			float beSet = Input.Down( "GuideSystem" ) ? 1f : 0f;
			GuideButtonTest.SetAnalog( beSet );
		}
		if ( MuteButtonTest.IsValid() )
		{
			float beSet = Input.Down( "GuideMute" ) ? 1f : 0f;
			MuteButtonTest.SetAnalog( beSet );
		}

		if ( PrimaryAttackTest.IsValid() )
		{
			PrimaryAttackTest.SetAnalog( Input.GetAnalog( InputAnalog.RightTrigger ) );
		}
		if ( AltAttackTest.IsValid() )
		{
			AltAttackTest.SetAnalog( Input.GetAnalog( InputAnalog.LeftTrigger ) );
		}

		if ( LeftBumperTest.IsValid() )
		{
			float beSet = Input.Down( "slotPrev" ) ? 1f : 0f;
			LeftBumperTest.SetAnalog( beSet );
		}
		if ( RightBumperTest.IsValid() )
		{
			float beSet = Input.Down( "slotNext" ) ? 1f : 0f;
			RightBumperTest.SetAnalog( beSet );
		}

		if ( Input.Pressed( "Attack1" ) )
		{
			Input.TriggerHaptics( 0f, 0f, 0f, 1f, 500 );
		}
		if ( Input.Pressed( "Attack2" ) )
		{
			Input.TriggerHaptics( 0f, 0f, 1f, 0f, 500 );
		}
		if ( Input.Pressed( "slotPrev" ) )
		{
			Input.TriggerHaptics( 1f, 0f, 0f, 0f, 500 );
		}
		if ( Input.Pressed( "slotNext" ) )
		{
			Input.TriggerHaptics( 0f, 1f, 0f, 0f, 500 );
		}

		//foreach(var paddleButtonTest in PaddleButtons)
		for(int i = 0;i<=3; i++)
		{
			//if(paddleButtonTest.IsValid())
			if(PaddleButtons[i].IsValid())
			{
				float beSet = Input.Down( String.Format("paddleButton_%d",i) ) ? 1f : 0f;
				PaddleButtons[i].SetAnalog( beSet );
			}
		}

		// InputMotionData? motionData = Input.MotionData;
		// if(motionData is not null)
		// {
		//   Vector3 acceleration = motionData.GetValueOrDefault; // Accelerometer
		//   Angles angularVelocity = motionData.Gyroscope; // Gyroscope
		//   // Process the data as needed...
		// }

		InputMotionData motionData = Input.MotionData;
		Vector3 acceleration = motionData.Accelerometer; // Accelerometer
		Angles gyroscope = motionData.Gyroscope; // Gyroscope
		if(GyroTest.IsValid())
		{
			//Log.Info( gyroscope );
			GyroTest.SetAnalog( acceleration );
			GyroTest.SpinAnalog( gyroscope.ToRotation() );
		}
	}
}
