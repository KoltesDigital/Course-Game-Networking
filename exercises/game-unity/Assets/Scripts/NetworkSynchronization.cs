using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

public class NetworkSynchronization : MonoBehaviour
{
	public NetworkManager networkManager;

	public string ownerId;
	public string ownerGameObjectId;

	public string gameObjectId;

	private PositionState positionState = new PositionState();

	private CancellationTokenSource cancellationTokenSource;

	private static int nextGameObjectId = 0;

	private bool IsOwner
	{
		get
		{
			return string.IsNullOrEmpty(ownerId);
		}
	}

	private void Start()
	{
		if (IsOwner)
		{
			gameObjectId = nextGameObjectId.ToString();
			++nextGameObjectId;

			UpdatePositionState();
			networkManager.EmitCreateGameObject(gameObjectId, positionState);

			cancellationTokenSource = new CancellationTokenSource();
			Task.Run(() => EmitPosition(cancellationTokenSource.Token), cancellationTokenSource.Token);
		}
		else
		{
			networkManager.SubscribeTransformEvent(ownerId, ownerGameObjectId, SetPositionState);
		}
	}

	private void OnDisable()
	{
		if (IsOwner)
		{
			cancellationTokenSource.Cancel();

			networkManager.EmitDestroyGameObject(gameObjectId);
		}
		else
		{
			networkManager.UnsubscribeTransformEvent(ownerId, ownerGameObjectId, SetPositionState);

		}
	}
	private async void EmitPosition(CancellationToken token)
	{
		try
		{
			for (; ; )
			{
				await Task.Delay(TimeSpan.FromSeconds(.5));

				if (token.IsCancellationRequested)
				{
					return;
				}

				lock (positionState)
				{
					networkManager.EmitSynchronizePosition(gameObjectId, positionState);
				}
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogError(ex);
		}
	}

	public void SetPositionState(PositionState newPositionState)
	{
		var position = new Vector2(newPositionState.x, newPositionState.y); ;

		RectTransform rt = GetComponent<RectTransform>();
		rt.anchoredPosition = position;

		lock (positionState)
		{
			positionState = newPositionState;
		}
	}

	private void Update()
	{
		UpdatePositionState();
	}

	private void UpdatePositionState()
	{
		RectTransform rt = GetComponent<RectTransform>();
		var position = rt.anchoredPosition;

		lock (positionState)
		{
			positionState.x = Mathf.RoundToInt(position.x);
			positionState.y = Mathf.RoundToInt(position.y);
		}
	}
}
