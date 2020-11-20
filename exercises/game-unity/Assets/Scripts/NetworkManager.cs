using UnityEngine;
using SocketIOClient;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
	public delegate void TransformHandler(PositionState positionState);

	private class SubscribedTransformHandler
	{
		public string clientId;
		public string gameObjectId;
		public TransformHandler handler;
	}

	public string serverUrl;
	public string serverRoomName;

	public Transform playerContainer;
	public GameObject playerPrefab;

	private SocketIO client;

	private List<SubscribedTransformHandler> subscribedTransformHandlers = new List<SubscribedTransformHandler>();

	private List<SocketIOResponse> creationMessages = new List<SocketIOResponse>();
	private List<SocketIOResponse> destructionMessages = new List<SocketIOResponse>();
	private List<SocketIOResponse> synchronizationMessages = new List<SocketIOResponse>();

	private object spawnLock = new object();
	private bool spawn = false;

	private async void OnEnable()
	{
		client = new SocketIO(serverUrl, new SocketIOOptions
		{
		});

		client.OnConnected += async (sender, e) =>
		{
			await client.EmitAsync("join", serverRoomName);

			lock (spawnLock)
			{
				spawn = true;
			}
		};

		client.On("create-game-object", message =>
		{
			lock (creationMessages)
			{
				creationMessages.Add(message);
			}
		});

		client.On("destroy-game-object", message =>
		{
			lock (destructionMessages)
			{
				destructionMessages.Add(message);
			}
		});

		client.On("synchronize-position", message =>
		{
			lock (synchronizationMessages)
			{
				synchronizationMessages.Add(message);
			}
		});

		await client.ConnectAsync();
	}

	private async void OnDisable()
	{
		foreach (Transform child in playerContainer)
		{
			Destroy(child.gameObject);
		}

		await client.DisconnectAsync();
		client = null;
	}

	public void SubscribeTransformEvent(string clientId, string gameObjectId, TransformHandler handler)
	{
		subscribedTransformHandlers.Add(new SubscribedTransformHandler
		{
			clientId = clientId,
			gameObjectId = gameObjectId,
			handler = handler,
		});
	}

	public bool UnsubscribeTransformEvent(string clientId, string gameObjectId, TransformHandler handler)
	{
		var index = 0;
		foreach (var transformHandler in subscribedTransformHandlers)
		{
			if (transformHandler.clientId == clientId && transformHandler.gameObjectId == gameObjectId && transformHandler.handler == handler)
			{
				subscribedTransformHandlers.RemoveAt(index);
				return true;
			}
			++index;
		}

		return false;
	}

	public void EmitCreateGameObject(string gameObjectId, PositionState positionState)
	{
		client.EmitAsync("create-game-object", gameObjectId, positionState);
	}

	public void EmitDestroyGameObject(string gameObjectId)
	{
		client?.EmitAsync("destroy-game-object", gameObjectId);
	}

	public void EmitSynchronizePosition(string gameObjectId, PositionState positionState)
	{
		client?.EmitAsync("synchronize-position", gameObjectId, positionState);
	}

	private void Update()
	{
		lock (spawnLock)
		{
			if (spawn)
			{
				spawn = false;

				var localPlayer = Instantiate(playerPrefab, playerContainer);

				var localNetworkSynchronization = localPlayer.AddComponent<NetworkSynchronization>();
				localNetworkSynchronization.networkManager = this;

				localPlayer.AddComponent<MouseFollower>();
			}
		}

		lock (creationMessages)
		{
			foreach (var message in creationMessages)
			{
				var ownerId = message.GetValue<string>(0);
				if (ownerId != client.Id)
				{
					var ownerGameObjectId = message.GetValue<string>(1);
					var positionState = message.GetValue<PositionState>(2);

					var remotePlayer = Instantiate(playerPrefab, playerContainer);

					var remoteNetworkSynchronization = remotePlayer.AddComponent<NetworkSynchronization>();
					remoteNetworkSynchronization.networkManager = this;
					remoteNetworkSynchronization.ownerId = ownerId;
					remoteNetworkSynchronization.ownerGameObjectId = ownerGameObjectId;
					remoteNetworkSynchronization.SetPositionState(positionState);
				}
			}
			creationMessages.Clear();
		}

		lock (destructionMessages)
		{
			foreach (var message in destructionMessages)
			{
				var ownerId = message.GetValue<string>(0);
				if (ownerId != client.Id)
				{
					var ownerGameObjectId = message.GetValue<string>(1);

					var components = playerContainer.GetComponentsInChildren<NetworkSynchronization>();
					foreach (var component in components)
					{
						if (component.ownerId == ownerId && component.ownerGameObjectId == ownerGameObjectId)
						{
							Destroy(component.gameObject);
							break;
						}
					}
				}
			}
			destructionMessages.Clear();
		}

		lock (synchronizationMessages)
		{
			foreach (var message in synchronizationMessages)
			{
				var clientId = message.GetValue<string>(0);
				var gameObjectId = message.GetValue<string>(1);
				var positionState = message.GetValue<PositionState>(2);

				foreach (var transformHandler in subscribedTransformHandlers)
				{
					if (transformHandler.clientId == clientId && transformHandler.gameObjectId == gameObjectId)
					{
						transformHandler.handler.Invoke(positionState);
					}
				}
			}
			synchronizationMessages.Clear();
		}
	}
}
