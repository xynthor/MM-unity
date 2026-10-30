using UnityEngine;

// This only attaches to the camera that is created when a scene has none.
public sealed class MMStandaloneFreeFly:MonoBehaviour {
 public float walkSpeed=19f;
 public float sprintMultiplier=3.4f;
 public float mouseSensitivity=2.2f;
#if ENABLE_LEGACY_INPUT_MANAGER
 void Update(){
  if(Input.GetMouseButton(1)){
   float yaw=Input.GetAxisRaw("Mouse X")*mouseSensitivity;
   float pitch=-Input.GetAxisRaw("Mouse Y")*mouseSensitivity;
   transform.Rotate(Vector3.up*yaw,Space.World);
   transform.Rotate(Vector3.right*pitch,Space.Self);
  }
  Vector3 input=new Vector3(Input.GetAxisRaw("Horizontal"),
   (Input.GetKey(KeyCode.E)?1f:0f)-(Input.GetKey(KeyCode.Q)?1f:0f),
   Input.GetAxisRaw("Vertical"));
  float speed=walkSpeed*(Input.GetKey(KeyCode.LeftShift)?sprintMultiplier:1f);
  transform.position+=transform.TransformDirection(input.normalized)*speed*Time.deltaTime;
 }
#endif
}
