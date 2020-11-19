class NetworkSynchronization extends Component {
	constructor() {
		super();

		this._intervalId = null;
		this.ownerId = null;
		this.ownerGameObjectId = null;

		this._setPositionHandler = (positionState) => {
			this.setPosition(positionState);
		};
	}

	onDetach() {
		super.onDetach();

		if (this.isOwner()) {
			socket.emit("destroy-game-object", this.gameObject.id);
			clearInterval(this._intervalId);
		} else {
			network.unsubscribeTransformEvent(
				this.ownerId,
				this.ownerGameObjectId,
				this._setPositionHandler
			);
		}
	}

	isOwner() {
		return this.ownerId === null;
	}

	start() {
		if (this.isOwner()) {
			socket.emit(
				"create-game-object",
				this.gameObject.id,
				this.getPositionState()
			);

			this._intervalId = setInterval(() => {
				socket.emit(
					"synchronize-position",
					this.gameObject.id,
					this.getPositionState()
				);
			}, 500);
		} else {
			network.subscribeTransformEvent(
				this.ownerId,
				this.ownerGameObjectId,
				this._setPositionHandler
			);
		}
	}

	getPositionState() {
		return {
			x: this.gameObject.x,
			y: this.gameObject.y,
		};
	}

	setPosition(positionState) {
		this.gameObject.x = positionState.x;
		this.gameObject.y = positionState.y;
	}
}
