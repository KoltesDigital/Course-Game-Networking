let mouseX, mouseY;

document.addEventListener("mousemove", (event) => {
	mouseX = event.pageX;
	mouseY = event.pageY;
});

class MouseFollower extends Component {
	update() {
		super.update();

		this.gameObject.x = mouseX;
		this.gameObject.y = mouseY;
	}
}
