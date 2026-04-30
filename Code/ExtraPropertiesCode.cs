using Sandbox;

// This is extra property attributes code sample. Again, let's learn s&box together, you and I!
// https://sbox.game/dev/doc/editor/property-attributes
// https://youtu.be/gY5PgW5pH90 See Carson's video for more, while we wait
// the official documentation to be complete first, lol!

public sealed class ExtraPropertiesCode : Component
{
	// Basics
	[Header( "This is General Properties" )]
	//<summary>
	// This is a jsDoc of a variable
	// </summary>
	[Property, Title( "String (1Line)" ), Description( "A single line string field" )] public string OneLineString { get; set; } = "OneLiner";
	//<summary>
	// Binary. Often represented as decimal but you can also use hex like 0x000001
	// </summary>
	[Property, Title( "Integer" ), Description( "Binary. Often represented as decimal but you can also use hex like 0x000001" )] public int AnInteger { get; set; } = 0x0123;
	[Property, Title( "Float" ), Description( "Single Precision Float Value" )] public float ASingleFloat { get; set; } = 1.5f;
	[Property, Title( "Double" ), Description( "Double Precision Float Value" )] public double ADoubleFloat { get; set; } = 1.5893748293794823789473f;
	//[Property, Title( "Quad" ), Description("Quadruple Precision Float Value")] public quad AQuadFloat { get; set; } = 1.5893748293794823789473f;
	[Property, Title( "Bool" ), Description( "Yes or No Value" )] public bool ABoolean { get; set; } = true;
	[Space]
	[Header( "And these are s&box Special Types" )]
	[Property, Title( "Curve" ), Description( "A Curve of what would be the `y` when its `x` is this" )] public Curve ACurve { get; set; }
	[Property, Title( "ActionGraph" ), Description( "Simple If-Else programming graph as value" )] public Action AnActGraph { get; set; }

	// Customizations
	[Header( "Customize How they'd look" )]
	[Property, Feature( "Advanced Customizations" ), Title( "Headers will snuck into Groups unless I add this Property to break it off" )] public bool HeaderWillSnuckInto { get; set; } = true;
	[Property, Feature( "Advanced Customizations" ), Group( "Spaced" ), Title( "Space Before" )] public int IHaveSpacedYou { get; set; } = 24;
	[Space]
	[Property, Feature( "Advanced Customizations" ), Group( "Spaced" ), Title( "Space After" )] public int IHaveBeenSpaced { get; set; } = 25;
	[Property, Feature( "Advanced Customizations" ), Group( "String Specific" ), Placeholder( "bla bla bla" ), TextArea] public string MultiLineString { get; set; } = "";
	[Property, Feature( "Advanced Customizations" ), Group( "String Specific" ), Placeholder( "materials/image/terry.png" ), ImageAssetPath] public string ImagePathString { get; set; } = "";
	[Property, Feature( "Advanced Customizations" ), Group( "String Specific" ), Placeholder( "scene/my_map.scene" ), ImageAssetPath] public string ScenePathString { get; set; } = "";
	[Property, Feature( "Advanced Customizations" ), Group( "String Specific" ), Placeholder( "audio/fart.wav" ), FilePath] public string FilePathString { get; set; } = "";
	[Property, Feature( "Advanced Customizations" ), Group( "String Specific" ), Placeholder( "jump" ), InputAction] public string InputActionString { get; set; } = "";
	[Property, Feature( "Advanced Customizations" ), Group( "String Specific" ), Placeholder( "Poppins" ), FontName] public string FontSelectString { get; set; } = "Poppins";

	[Property, Feature( "Advanced Customizations" ), Group( "Curve Specific" ), TimeRange( 0f, 100f )] public Curve TimeRangeCurve { get; set; } = new Curve();
	[Property, Feature( "Advanced Customizations" ), Group( "Curve Specific" ), ValueRange( 0f, 100f )] public Curve ValueRangeCurve { get; set; } = new Curve();

	[Property, Feature( "Advanced Customizations" ), Group( "ActionGraph Specific" ), SingleAction, Description( "You can make an `Action` variable only accept one, instead of by default, multiple in list." )] public Action SingleActionGraph { get; set; }

	[Property, Feature( "Advanced Customizations" ), ToggleGroup( "ReadOnly" ), Title( "Read Only" )] bool ReadOnly { get; set; } = true;
	[Property, Feature( "Advanced Customizations" ), Group( "ReadOnly" ), ReadOnly] public string ReadOnlyString { get; set; } = "You can't edit me lol";
	[Property, Feature( "Advanced Customizations" ), Group( "ReadOnly" ), ReadOnly] public bool ReadOnlyBool { get; set; } = true;
	[Property, Feature( "Advanced Customizations" ), Group( "ReadOnly" ), ReadOnly] public float ReadOnlyFloat { get; set; } = 6.7f;
	[Property, Feature( "Advanced Customizations" ), Group( "ReadOnly" ), ReadOnly] public double ReadOnlyDouble { get; set; } = 67.443322f;
	[Property, Feature( "Advanced Customizations" ), Group( "ReadOnly" ), ReadOnly] public int ReadOnlyInteger { get; set; } = 0x067;

	[Property, Feature( "Advanced Customizations" ), Group( "Wide Mode" ), WideMode] public float WideFloat = 99999f;
	[Property, Feature( "Advanced Customizations" ), Group( "Wide Mode" ), WideMode] public double WideDouble = 99999f;
	[Property, Feature( "Advanced Customizations" ), Group( "Wide Mode" ), WideMode] public int WideInteger = 1234567890;
	[Property, Feature( "Advanced Customizations" ), Group( "Wide Mode" ), WideMode] public string WideString = "AAAAAAAAAAAAAAAAAAAA!!!";
	[Property, Feature( "Advanced Customizations" ), Group( "Wide Mode" ), WideMode, TextArea] public string WideText = "ASDHFLAUIHWERIUFHASLSUDUHFKLJASHDFJKLAHSIULGHALIERHGAKLJSHDFGKJLAHSDKJLFHALSKJDHFLAKJDHSFKLJ!!!";

	public struct MyStruct
	{
		public MyStruct( string yourNameIs = "Jane Doe", int toSeatOn = 1 )
		{
			Name = yourNameIs;
			SeatNumber = toSeatOn;
		}
		[KeyProperty] string Name { get; set; } = "Jane Doe";
		[KeyProperty] int SeatNumber { get; set; } = 2;
		[KeyProperty] bool hasSomething { get; set; } = true;
		[KeyProperty] bool hasSomethang { get; set; } = false;
		float Power { get; set; } = 5f;
		float Strength { get; set; } = 10f;
	}
	[Property, Feature( "Advanced Customizations" ), Group( "Structs" ), Title( "Cramped Struct" )] public MyStruct BioData { get; set; } = new MyStruct( "Vostamril" );
	[Property, Feature( "Advanced Customizations" ), Group( "Structs" ), Title( "Inline Struct" ), InlineEditor] public MyStruct BioDataUnCramped { get; set; } = new MyStruct( "Milky" );

	// Enum
	public enum TheEnum
	{
		[Icon( "😎" ), Description( "I'm Foo 👆" )] foo = 0,
		[Icon( "add_reaction" ), Description( "I'm Bar ⚠️" )] bar = 1,
		[Icon( "agriculture" ), Description( "I'm Baz 💥" )] baz = 2,
		[Icon( "@" ), Description( "I'm Quix ♥️" )] quix = 8,
	}
	[Flags]
	public enum TheEnumMulti
	{
		[Icon( "😎" ), Description( "I'm Foo 🫀" )] foo = 0,
		[Icon( "!?" ), Description( "I'm Bar 🫁" )] bar = 1,
		[Icon( "agriculture" ), Description( "I'm Baz 🧠" )] baz = 2,
		[Icon( "admin_panel_settings" ), Description( "I'm Quix 🦷" )] quix = 8,
	}
	[Property, FeatureEnabled( "Enumerations" )] public bool EnableEnum { get; set; } = true;
	[Header( "These are Enums" )]
	// [InfoBox("aaaa")]
	[Property, Feature( "Enumerations" ), Title( "The Enum yey" )] public bool DummyEnumed { get; set; } // wa
	[Property, Feature( "Enumerations" ), Group( "Single Selecting" )] public TheEnum SingleSelectOption { get; set; } = TheEnum.bar;
	[Property, Feature( "Enumerations" ), Group( "Multi Selecting" )] public TheEnumMulti MultiSelectOption { get; set; }

	// Show Hide
	[Property, FeatureEnabled( "Show & Hide" )] public bool FeatureShowHide { get; set; } = true;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" )] public bool EnableTheSliders { get; set; } = true;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" ), Range( 0f, 200f ), Step( 1 )] public float Health { get; set; } = 100f;
	// the `Range( 0f, 200f, 1)` is deprecated lmao!!! use `Range(0f, 200f), Step(1)` instead, separated!
	bool _canShowBoostean => Health >= 100;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" ), ShowIf( nameof( _canShowBoostean ), true )] public float Boostean { get; set; } = 1000f;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" ), HideIf( nameof( _canShowBoostean ), true )] public float Crutches { get; set; } = 500f;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" ), Range( 0f, 100f, true, false ), Step( 2 )] public float Stepper { get; set; } = 100f;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" ), Range( 0f, 100f, true, true ), Step( 2 )] public float StepperSlide { get; set; } = 100f;
	[Property, Feature( "Show & Hide" ), Group( "Sliders" ), Range( 0f, 100f, false, true ), Step( 2 )] public float StepperSlideUnclamp { get; set; } = 100f;

	// Validation
	bool IsValidTest => ValidationTest >= 50f;
	public bool JustValid { get; set; } = true;
	[Property, Feature( "Show & Hide" ), Group( "Validation" ), Range( 0f, 200f ), Step( 1 ), Validate( nameof( IsValidTest ), "Uh Oh", LogLevel.Warn ), Validate( "IsValidTest", "Hello", LogLevel.Info )] public float ValidationTest = 50f;

	// Advanced
	[Property, Feature( "Show & Hide" ), Group( "Flag \"Advanced\"" )] public bool EnableAdvanced { get; set; } = true;
	[Property, Feature( "Show & Hide" ), Group( "Flag \"Advanced\"" ), Advanced] public string ThisIsTooAdvancedForYou { get; set; } = "YEEEAAAAAAAAA";

	// Require Component
	[Property, Feature( "Show & Hide" ), Group( "Requires Component" ),RequireComponent,Description("`RequireComponent` flag is used on a Component variable, to make it automatically adds itself & make it refer to that when it doesn't have one yet.")] public ExtraRequiredCode IRequireYouToBeHere { get; set; }
}

// I admit. It's not as great as Unity right now. But c'mon, have some mercy, they're just getting started bruh!
// Coz at least how this guy had it too!

// If you ask me, Godot not only had no official pre-builts like s&box (wtf, how am I supposed to do with KinematicBody), they socially blundered!
// I don't need to explain Unity & Unreal and any other popular competitors out there.
// And s&box as of now only allows at least Steam Publications first before other platforms.
// Pls change! Godot has gone mismanaged like every Linux politics out there!
// They even violated DNB to the contributors at their GitHub coz speaking out against this unecessary moves, yikes!
// Using Google Form in PR deperation, asking too sensitive data probably would be used to insult the truth whistleblowers, ouch!

// Oh my God, you too?!
// Rabbithole_xyz
// Urgh!, Unbelievable. Don't blame me if one day we IRL'd DNB nation you are required to POC License just to Social Media
// Or outright talking to strangers.
// Or worse yet, we at last agreed & hence assumed this bill of `Art nothing to do with artist` that has been sitting since May 2024,
// Wow! 2 centuries?!
