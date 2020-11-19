const socket = io(SERVER_URL);
socket.emit("join", SERVER_ROOM_NAME);

let network;

{
	const _synchronizePositionSubscribers = [];
	const _synchronizedGameObjects = {};

	socket.on("create-game-object", function (
		clientId,
		gameObjectId,
		positionState
	) {
		if (clientId !== socket.id) {
			const gameObject = scene.createGameObject(gameObjectId);
			_synchronizedGameObjects[clientId + "/" + gameObjectId] = gameObject;

			gameObject.addComponent(new Renderer());

			const networkSynchronization = gameObject.addComponent(
				new NetworkSynchronization()
			);
			networkSynchronization.ownerId = clientId;
			networkSynchronization.ownerGameObjectId = gameObjectId;
			networkSynchronization.setPosition(positionState);
		}
	});

	socket.on("destroy-game-object", function (clientId, gameObjectId) {
		if (clientId !== socket.id) {
			const gameObject =
				_synchronizedGameObjects[clientId + "/" + gameObjectId];
			if (gameObject) {
				scene.destroyGameObject(gameObject);
				delete _synchronizedGameObjects[clientId + "/" + gameObjectId];
			}
		}
	});

	socket.on("synchronize-position", function (
		clientId,
		gameObjectId,
		positionState
	) {
		_synchronizePositionSubscribers.forEach((subscriber) => {
			if (
				subscriber.clientId === clientId &&
				subscriber.gameObjectId === gameObjectId
			) {
				subscriber.handler(positionState);
			}
		});
	});

	network = {
		subscribeTransformEvent: (clientId, gameObjectId, handler) => {
			_synchronizePositionSubscribers.push({
				clientId,
				gameObjectId,
				handler,
			});
		},

		unsubscribeTransformEvent: (clientId, gameObjectId, handler) => {
			for (let i = 0; i < _synchronizePositionSubscribers.length; ++i) {
				const subscriber = _synchronizePositionSubscribers[i];
				if (
					subscriber.clientId === clientId &&
					subscriber.gameObjectId === gameObjectId &&
					subscriber.handler === handler
				) {
					_synchronizePositionSubscribers.splice(i, 1);
					return true;
				}
			}
			return false;
		},
	};
}
