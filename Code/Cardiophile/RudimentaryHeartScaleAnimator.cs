namespace Sandbox;

public sealed class RudimentaryHeartScaleAnimator : Component
{
	[Property, Feature( "Properties" )] public float enlargeTo = 1.2f;
	[Property, Feature( "Properties" )] public float relaxTo = 1f;
	[Property, Feature( "Properties" )] public bool reverseBeat = true;

	[Property, RequireComponent] HeartOrgan Itself { get; set; }
	[Property] GameObject ModelObject { get; set; }

	protected override void OnUpdate()
	{
		if(Itself.IsValid())
		{
			if(ModelObject.IsValid())
			{
				// frac were 0.2f fixed

				if ( reverseBeat )
				{
					// if ( Itself.Lub ) ModelObject.WorldScale = enlargeTo;
					if ( Itself.Lub ) ModelObject.WorldScale = Vector3.Lerp(ModelObject.WorldScale,relaxTo,.02f);
					// else ModelObject.WorldScale = relaxTo;
					else ModelObject.WorldScale =  Vector3.Lerp(ModelObject.WorldScale,enlargeTo,.02f);
				}
				else{
					// if ( Itself.Lub ) ModelObject.WorldScale = enlargeTo;
					if ( Itself.Lub ) ModelObject.WorldScale = Vector3.Lerp(ModelObject.WorldScale,enlargeTo,.02f);
					// else ModelObject.WorldScale = relaxTo;
					else ModelObject.WorldScale =  Vector3.Lerp(ModelObject.WorldScale,relaxTo,.02f);
				}


				//DONE: Lerp!!!
				// TODO: Woman 3d model, prioritize furry ones, such as horse thyren or subspecies like pony hasbro
			}
		}
	}
}
