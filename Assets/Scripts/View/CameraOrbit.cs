using UnityEngine;

namespace ChessBattle.View
{
    public class CameraOrbit : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 10.0f;
        public float Sensitivity = 5.0f;
        public float ZoomSensitivity = 2.0f;
        public float MinDistance = 5.0f;
        public float MaxDistance = 20.0f;
        
        [Header("Cinematic")]
        public bool AutoRotate = false;
        public float AutoRotateSpeed = 10.0f;

        private float _currentX = 0.0f;
        private float _currentY = 45.0f;
        private float _currentDistance;

        private void Start()
        {
            if (Target == null)
            {
                // Try to find board center
                GameObject board = GameObject.Find("BoardView");
                if (board != null) Target = board.transform;
            }

            if (Target != null)
            {
                // Init from current transform to prevent jumping
                _currentDistance = Vector3.Distance(transform.position, Target.position);
                Distance = _currentDistance; 

                Vector3 angles = transform.eulerAngles;
                _currentX = angles.y;
                _currentY = angles.x;
            }
            else
            {
                 _currentDistance = Distance;
            }
        }

        private bool _isOrbiting = false;

        private void LateUpdate()
        {
            if (Target == null) return;

            // Check for activation
            bool inputActive = Input.GetMouseButton(1) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.001f;
            
            if (!_isOrbiting)
            {
                if (inputActive || AutoRotate)
                {
                    _isOrbiting = true;
                    SyncState(); // Sync one last time to ensure no jump
                }
                else
                {
                    return; // Do NOTHING. Stay exactly where placed in Editor.
                }
            }

            // Input
            if (Input.GetMouseButton(1)) // Right Mouse to Orbit
            {
                _currentX += Input.GetAxis("Mouse X") * Sensitivity;
                _currentY -= Input.GetAxis("Mouse Y") * Sensitivity;
                AutoRotate = false; 
            }
            else if (AutoRotate)
            {
                _currentX += AutoRotateSpeed * Time.deltaTime;
            }
            
            if (Input.GetMouseButton(1) || AutoRotate)
            {
                 _currentY = Mathf.Clamp(_currentY, 10.0f, 80.0f);
            }

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _currentDistance -= scroll * ZoomSensitivity;
            _currentDistance = Mathf.Clamp(_currentDistance, MinDistance, MaxDistance);

            // Calculation
            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 position = Target.position + (rotation * Vector3.back * _currentDistance);

            transform.rotation = rotation;
            transform.position = position;
        }

        private void SyncState()
        {
             if (Target == null) return;
             _currentDistance = Vector3.Distance(transform.position, Target.position);
             Distance = _currentDistance; 

             Vector3 angles = transform.eulerAngles;
             _currentX = angles.y;
             _currentY = angles.x;
        }
    }
}
