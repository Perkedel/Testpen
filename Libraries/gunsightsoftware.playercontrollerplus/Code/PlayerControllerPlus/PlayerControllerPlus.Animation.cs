using Sandbox.Audio;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox;

public sealed partial class PlayerControllerPlus : Component
{
	SkinnedModelRenderer _renderer;

	[FeatureEnabled( "Animator", Icon = "sports_martial_arts" )]
	public bool UseAnimatorControls { get; set; } = true;

	/// <summary>
	/// The body will usually be a child object with SkinnedModelRenderer
	/// </summary>
	[Property, Feature( "Animator" )]
	public SkinnedModelRenderer Renderer
	{
		get => _renderer;
		set
		{
			if ( _renderer == value ) return;

			DisableAnimationEvents();

			_renderer = value;

			EnableAnimationEvents();
		}
	}

	/// <summary>
	/// If true we'll show the "create body" button
	/// </summary>
	public bool ShowCreateBodyRenderer => UseAnimatorControls && Renderer is null;

	[Button( icon: "add" )]
	[Property, Feature( "Animator" ), Tint( EditorTint.Green ), ShowIf( "ShowCreateBodyRenderer", true )]
	public void CreateBodyRenderer()
	{
		var body = new GameObject( true, "Body" );
		body.Parent = GameObject;

		Renderer = body.AddComponent<SkinnedModelRenderer>();
		Renderer.Model = Model.Load( "models/citizen/citizen.vmdl" );
	}

	[Property, Feature( "Animator" )] public float RotationAngleLimit { get; set; } = 45.0f;
	[Property, Feature( "Animator" )] public float RotationSpeed { get; set; } = 1.0f;

	[Property, Feature( "Animator" ), Group( "Footsteps" )] public bool EnableFootstepSounds { get; set; } = true;
	[Property, Feature( "Animator" ), Group( "Footsteps" )] public float FootstepVolume { get; set; } = 1;


	[Property, Feature( "Animator" ), Group( "Footsteps" )] public MixerHandle FootstepMixer { get; set; }

	/// <summary>
	/// How strongly to look in the eye direction with our eyes
	/// </summary>
	[Property, Feature( "Animator" ), Group( "Aim" ), Order( 1001 ), Range( 0, 1 )] public float AimStrengthEyes { get; set; } = 1;

	/// <summary>
	/// How strongly to turn in the eye direction with our head
	/// </summary>
	[Property, Feature( "Animator" ), Group( "Aim" ), Order( 1002 ), Range( 0, 1 )] public float AimStrengthHead { get; set; } = 1;


	/// <summary>
	/// How strongly to turn in the eye direction with our body
	/// </summary>
	[Property, Feature( "Animator" ), Group( "Aim" ), Order( 1003 ), Range( 0, 1 )] public float AimStrengthBody { get; set; } = 1;

	[Property, Feature( "Extended" ), Group( "Animator" )] public bool UseWorldVelocityForAnimation { get; set; }

	[Property, Feature( "Extended" ), Group( "Animator" ), Range( 0, 2 )] public float AnimationSmoothTime { get; set; } = 0.6f;

	/// <summary>
	/// How fast the character model blends into the crouch pose. Higher = faster. Default (5) is stock.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "Animator" ), Range( 1, 50 )] public float CrouchAnimationSpeed { get; set; } = 5;

	/// <summary>
	/// How fast the camera height adjusts when crouching. Higher = faster. Default (50) is nearly instant.
	/// </summary>
	[Property, Feature( "Extended" ), Group( "Camera" )] public float CrouchCameraSpeed { get; set; } = 50;

	void EnableAnimationEvents()
	{
		if ( Renderer is null ) return;
		Renderer.OnFootstepEvent -= OnFootstepEvent;
		Renderer.OnFootstepEvent += OnFootstepEvent;
	}

	void DisableAnimationEvents()
	{
		if ( Renderer is null ) return;
		Renderer.OnFootstepEvent -= OnFootstepEvent;
	}

	/// <summary>
	/// Update the animation for this renderer. This will update the body rotation etc too.
	/// </summary>
	public void UpdateAnimation( SkinnedModelRenderer renderer )
	{
		if ( !renderer.IsValid() ) return;

		// TODO: move to MoveModePlus?
		// TODO: frame rate dependent

		renderer.LocalPosition = bodyDuckOffset;
		bodyDuckOffset = bodyDuckOffset.LerpTo( 0, Time.Delta * (ExtendedFeaturesEnabled ? CrouchAnimationSpeed : 5.0f) );

		Mode?.UpdateAnimator( renderer );
	}

	SkinnedModelRenderer _shadowHeadRenderer;
	List<SkinnedModelRenderer> _shadowClothingRenderers = new();
	bool _trueFirstPersonActive;

	void UpdateBodyVisibility()
	{
		if ( !UseCameraControls ) return;
		if ( Scene.Camera is not CameraComponent cam ) return;

		// are we looking through this GameObject?
		bool viewer = !ThirdPerson;
		viewer = viewer && HideBodyInFirstPerson;
		viewer = viewer && !IsProxy;

		if ( !IsProxy && _cameraDistance < 20 )
		{
			viewer = true;
		}

		if ( IsProxy )
		{
			viewer = false;
		}

		bool wantsTrueFirstPerson = ExtendedFeaturesEnabled
			&& TrueFirstPerson
			&& !ThirdPerson
			&& !HideBodyInFirstPerson
			&& !IsProxy;

		if ( wantsTrueFirstPerson && !_trueFirstPersonActive )
		{
			EnableTrueFirstPerson();
		}
		else if ( !wantsTrueFirstPerson && _trueFirstPersonActive )
		{
			DisableTrueFirstPerson();
		}

		if ( _trueFirstPersonActive )
		{
			UpdateShadowClothing();
			viewer = false;
		}

		var go = Renderer?.GameObject ?? GameObject;

		if ( go.IsValid() )
		{
			go.Tags.Set( "viewer", viewer );
		}
	}

	void EnableTrueFirstPerson()
	{
		if ( !Renderer.IsValid() ) return;

		_trueFirstPersonActive = true;

		Renderer.SetBodyGroup( "head", 1 );

		var shadowGo = new GameObject( Renderer.GameObject, true, "_ShadowHead" );
		_shadowHeadRenderer = shadowGo.Components.Create<SkinnedModelRenderer>();
		_shadowHeadRenderer.Model = Renderer.Model;
		_shadowHeadRenderer.BoneMergeTarget = Renderer;
		_shadowHeadRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		_shadowHeadRenderer.UseAnimGraph = false;
		_shadowHeadRenderer.SetBodyGroup( "head", 0 );
		_shadowHeadRenderer.SetBodyGroup( "body", 1 );
		_shadowHeadRenderer.SetBodyGroup( "legs", 1 );
		_shadowHeadRenderer.SetBodyGroup( "feet", 1 );

		UpdateShadowClothing();
	}

	void DisableTrueFirstPerson()
	{
		_trueFirstPersonActive = false;

		if ( Renderer.IsValid() )
		{
			Renderer.SetBodyGroup( "head", 0 );
		}

		if ( _shadowHeadRenderer.IsValid() )
		{
			_shadowHeadRenderer.GameObject.Destroy();
			_shadowHeadRenderer = null;
		}

		foreach ( var clothing in _processedClothing )
		{
			if ( clothing.IsValid() )
				clothing.RenderType = ModelRenderer.ShadowRenderType.On;
		}
		_processedClothing.Clear();

		foreach ( var r in _shadowClothingRenderers )
		{
			if ( r.IsValid() )
				r.GameObject.Destroy();
		}
		_shadowClothingRenderers.Clear();
	}

	static readonly HashSet<Clothing.ClothingCategory> _headCategories = new()
	{
		Clothing.ClothingCategory.Hat, Clothing.ClothingCategory.HatCap,
		Clothing.ClothingCategory.HatBeanie, Clothing.ClothingCategory.HatFormal,
		Clothing.ClothingCategory.HatCostume, Clothing.ClothingCategory.HatUniform,
		Clothing.ClothingCategory.HatSpecial,
		Clothing.ClothingCategory.Hair, Clothing.ClothingCategory.HairShort,
		Clothing.ClothingCategory.HairMedium, Clothing.ClothingCategory.HairLong,
		Clothing.ClothingCategory.HairUpdo, Clothing.ClothingCategory.HairSpecial,
		Clothing.ClothingCategory.Facial,
		Clothing.ClothingCategory.FacialHairMustache, Clothing.ClothingCategory.FacialHairBeard,
		Clothing.ClothingCategory.FacialHairStubble, Clothing.ClothingCategory.FacialHairSideburns,
		Clothing.ClothingCategory.FacialHairGoatee,
		Clothing.ClothingCategory.Eyewear,
		Clothing.ClothingCategory.GlassesEye, Clothing.ClothingCategory.GlassesSun,
		Clothing.ClothingCategory.GlassesSpecial,
		Clothing.ClothingCategory.Eyes, Clothing.ClothingCategory.Eyebrows,
		Clothing.ClothingCategory.Eyelashes,
		Clothing.ClothingCategory.MakeupLips, Clothing.ClothingCategory.MakeupEyeshadow,
		Clothing.ClothingCategory.MakeupEyeliner, Clothing.ClothingCategory.MakeupHighlighter,
		Clothing.ClothingCategory.MakeupBlush, Clothing.ClothingCategory.MakeupSpecial,
		Clothing.ClothingCategory.Headwear,
		Clothing.ClothingCategory.HeadTech, Clothing.ClothingCategory.HeadBand,
		Clothing.ClothingCategory.HeadJewel, Clothing.ClothingCategory.HeadSpecial,
		Clothing.ClothingCategory.PierceNose, Clothing.ClothingCategory.PierceEyebrow,
		Clothing.ClothingCategory.PierceSpecial,
		Clothing.ClothingCategory.EarringStud, Clothing.ClothingCategory.EarringDangle,
		Clothing.ClothingCategory.EarringSpecial,
		Clothing.ClothingCategory.ComplexionFreckles, Clothing.ClothingCategory.ComplexionScars,
		Clothing.ClothingCategory.ComplexionAcne,
	};

	bool IsHeadClothing( SkinnedModelRenderer clothing )
	{
		if ( clothing?.Model is null ) return false;

		var modelPath = clothing.Model.ResourcePath;
		if ( string.IsNullOrEmpty( modelPath ) ) return false;

		foreach ( var item in ResourceLibrary.GetAll<Clothing>() )
		{
			if ( item.Model == modelPath || item.HumanAltModel == modelPath || item.HumanAltFemaleModel == modelPath )
			{
				return _headCategories.Contains( item.Category );
			}
		}

		return false;
	}

	HashSet<SkinnedModelRenderer> _processedClothing = new();

	void UpdateShadowClothing()
	{
		if ( !Renderer.IsValid() || !_trueFirstPersonActive ) return;

		foreach ( var child in Renderer.GameObject.Children )
		{
			if ( !child.IsValid() ) continue;
			if ( child.Name == "_ShadowHead" ) continue;

			var clothing = child.Components.Get<SkinnedModelRenderer>();
			if ( !clothing.IsValid() ) continue;
			if ( _processedClothing.Contains( clothing ) ) continue;

			_processedClothing.Add( clothing );

			if ( IsHeadClothing( clothing ) )
			{
				clothing.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		}
	}

	void CleanupTrueFirstPerson()
	{
		if ( _trueFirstPersonActive )
			DisableTrueFirstPerson();
	}
}
