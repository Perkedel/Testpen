using System.Runtime.CompilerServices;
// using BaseFileSystem;

namespace Sandbox;

public sealed class AnCookieClicker : Component
{
	// Cookie Clicker simple base
	[Property] public double Cookies { get; set; } = 0;
	[Property] public float Multiplier { get; set; } = 1f;

	float RudimentarySaveIn { get; set; } = 10f;
	float RudimentarySaveCountdown { get; set; } = 10f;

	public void ClickTheCookie()
	{
		// Cookies++;
		Cookies += Multiplier;
	}

	public void AddMultiplierBy( float Number )
	{
		Multiplier += Number;
	}

	public void ResetMultiplier()
	{
		Multiplier = 1f;
	}

	public void Save()
	{
		// AnCookieClickerData anData = new AnCookieClickerData( Cookies );
		// Log.Info( "try save" );
		// FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieClicks.json", anData );
		// FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieClicks.json", new AnCookieClickerData( (int) Math.Round(Cookies) ) );
		// FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieClicks.json", new AnCookieClickerData(22) );
		// FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieaClicks.json", new AnCookieClickerData(Cookies) );
		// bro, s&box didn't tell me the error enough that I just had to add `<>` on that function wtf
		// nvm spoke too soon
		// FileSystem.Data.WriteJson<DataStructureSample>( "dataStructTestRaw.json", new DataStructureSample($"Cookies {Cookies}") );
		// I found it! do not open the file! in Zed it does not let go!
		FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieClicks.json", new AnCookieClickerData( Cookies ) );
		// took me ages to figure out!
		// Log.Info( "wtf work lah" );
	}

	public AnCookieClickerData Load()
	{
		return FileSystem.Data.ReadJson<AnCookieClickerData>( "anCookieClicks.json" , new AnCookieClickerData((int) Math.Round(Cookies)));
	}

	protected override void OnStart()
	{
		base.OnStart();
		// load save!
		AnCookieClickerData loading = Load();
		Cookies = loading.Cookies; // what?!
		// Save();
		// FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieClicks.json", new AnCookieClickerData( (uint) Math.Round(Cookies) ) );
		// FileSystem.Data.WriteJson<AnCookieClickerData>( "anCookieClicks.json", new AnCookieClickerData(22) );
		FileSystem.Data.WriteJson<DataStructureSample>( "dataStructTestRaw.json", new DataStructureSample("sdfkalsdjflkasd") );
	}

	protected override void OnUpdate()
	{
		if ( Multiplier < 0.1f )
		{
			Multiplier = 0.1f;
		}

		// TODO: JSON save
		if(RudimentarySaveCountdown <= 0){
			// Log.Info( "bruh" );
			Save();
			RudimentarySaveCountdown = RudimentarySaveIn;
		} else {
			RudimentarySaveCountdown -= Time.Delta;
		}
	}

	protected override void OnDestroy()
	{
		Save();
		base.OnDestroy();
	}
}
