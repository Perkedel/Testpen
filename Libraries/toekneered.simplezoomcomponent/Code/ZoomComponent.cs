using Sandbox;
using System;
using System.Linq;

/// <summary>
/// Zoom Component that uses FOV to switch between third and first person.
/// </summary>
[Title( "Simple Zoom Component" )]
public class ZoomComponent : Component
{
	[Property, ReadOnly] public EFovState FovState { get; set; } = EFovState.FirstPerson;
	[Property] public string ZoomInActionName { get; set; } = "Zoom In";
	[Property] public string ZoomOutActionName { get; set; } = "Zoom Out";
	[Property] public string ChangeViewActionName { get; set; } = "view";
	[Property] public float ZoomStep { get; set; } = 3.0f;
	[Property] public float MinFov { get; set; } = 30.0f;
	[Property] public float MaxFov { get; set; } = 120.0f;
	[Property, ReadOnly] private float _targetFovDiff = 0.0f;
	[Property, ReadOnly] private float _baseFov = 0.0f;
	[Property, ReadOnly] private bool _canZoom = true;
	private PlayerController _playerController = null;
	private CameraComponent _camera = null;

	public enum EFovState { FirstPerson, FirstToThird, ThirdPerson, ThirdToFirst };

	protected override void OnStart()
	{
		base.OnStart();

		_playerController ??= GetComponent<PlayerController>();
		_camera ??= Scene.GetAllComponents<CameraComponent>().FirstOrDefault(x => x.Enabled);
		if(_camera != null)
			_baseFov = _camera.FieldOfView;
	}

	protected override void OnUpdate()
	{
		HandleInput();
	}

	private void HandleInput()
	{
		if ( Input.Down( ChangeViewActionName ) )
		{
			FovState = _playerController.ThirdPerson ? EFovState.ThirdPerson : EFovState.FirstPerson;
		}

		if ( _canZoom )
		{
			switch ( FovState )
			{
				case EFovState.ThirdPerson:
					float input = 0f;

					if (Input.Down( ZoomInActionName ))  input -= ZoomStep;
					if (Input.Down( ZoomOutActionName )) input += ZoomStep;

					_targetFovDiff += input * ZoomStep;
					_targetFovDiff = Math.Clamp(_targetFovDiff, MinFov - _baseFov - 0.0001f, MaxFov - _baseFov);

					float targetFov = _baseFov + _targetFovDiff;

					if (targetFov < MinFov)
					{
						SetFovState(EFovState.ThirdToFirst);
					}

					_camera.FieldOfView = targetFov;

					break;
				case EFovState.FirstPerson:
					if ( Input.Down( ZoomOutActionName ) )
					{
						SetFovState( EFovState.FirstToThird );
					}
					break;
				case EFovState.FirstToThird:
					break;
				case EFovState.ThirdToFirst:
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof(FovState), FovState, null );
			}
		}
	}

	private void SetFovState( EFovState fovState )
	{
		switch ( fovState )
		{
			case EFovState.FirstPerson:
				break;
			case EFovState.FirstToThird:
				_canZoom = false;
				_targetFovDiff = MinFov - _baseFov;
				_camera.FieldOfView = MinFov;
				_playerController.ThirdPerson = true;
				FovState = EFovState.ThirdPerson;

				break;
			case EFovState.ThirdPerson:
				break;
			case EFovState.ThirdToFirst:
				_canZoom = false;
				_targetFovDiff = 0.0f;
				_camera.FieldOfView = _baseFov;
				_playerController.ThirdPerson = false;
				FovState = EFovState.FirstPerson;

				break;
			default:
				throw new ArgumentOutOfRangeException( nameof(fovState), fovState, null );
		}

		_canZoom = true;
	}
}

