using UnityEngine;

public class MouseFollower : MonoBehaviour
{
	void Update()
	{
		var position = Input.mousePosition;
		position.y = Screen.height - position.y;

		RectTransform rt = GetComponent<RectTransform>();
		rt.anchoredPosition = position;
	}
}
