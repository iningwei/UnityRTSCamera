using UnityEngine;
using System.Collections;

namespace RTS_Cam
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("RTS Camera")]
    public class RTS_Camera : MonoBehaviour
    {

        #region Foldouts

#if UNITY_EDITOR

        public int lastTab = 0;

        public bool movementSettingsFoldout;
        public bool zoomingSettingsFoldout;
        public bool rotationSettingsFoldout;
        public bool heightSettingsFoldout;
        public bool mapLimitSettingsFoldout;
        public bool targetingSettingsFoldout;
        public bool inputSettingsFoldout;

#endif

        #endregion

        private Transform m_Transform; //camera tranform
        public bool useFixedUpdate = false; //use FixedUpdate() or Update()

        #region Movement

        public float keyboardMovementSpeed = 5f; //speed with keyboard movement
        public float screenEdgeMovementSpeed = 3f; //spee with screen edge movement
        public float followingSpeed = 5f; //speed when following a target
        public float rotationSped = 3f;
        public float panningSpeed = 10f;
        public float mouseRotationSpeed = 10f;

        #endregion

        #region Height

        public bool initWithOriginHeight = true;//true means camera will have it init view,otherwise will move to minHeight's associate pos.
        bool originHeightSetted = false;
        public LayerMask groundMask = -1; //layermask of ground or other objects that affect height

        public float maxHeight = 10f; //maximal height
        public float minHeight = 15f; //minimnal height
        public float heightDampening = 5f;
        public float keyboardZoomingSensitivity = 2f;
        public float scrollWheelZoomingSensitivity = 25f;

        private float zoomPos = 0; //value in range (0, 1) used as t in Matf.Lerp

        #endregion

        #region MapLimits

        public bool limitMap = true;
        public float limitXMin = -50f; //x limit of map
        public float limitXMax = 50f;
        public float limitYMin = -50f; //z limit of map
        public float limitYMax = 50f;

        #endregion

        #region Targeting

        public Transform targetFollow; //target to follow
        public Vector3 targetOffset;

        /// <summary>
        /// are we following target
        /// </summary>
        public bool FollowingTarget
        {
            get
            {
                return targetFollow != null;
            }
        }

        #endregion

        #region Input

        public bool useScreenEdgeInput = true;
        public float screenEdgeBorder = 25f;

        public bool useKeyboardInput = true;
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";

        public bool usePanning = true;
        public KeyCode panningKey = KeyCode.Mouse2;

        public bool useKeyboardZooming = true;
        public KeyCode zoomInKey = KeyCode.E;
        public KeyCode zoomOutKey = KeyCode.Q;

        public bool useScrollwheelZooming = true;
        public string zoomingAxis = "Mouse ScrollWheel";

        public bool useKeyboardRotation = true;
        public KeyCode rotateRightKey = KeyCode.X;
        public KeyCode rotateLeftKey = KeyCode.Z;

        public bool useMouseRotation = true;
        public KeyCode mouseRotationKey = KeyCode.Mouse1;

        private Vector2 KeyboardInput
        {
            get { return useKeyboardInput ? new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis)) : Vector2.zero; }
        }

        private Vector2 MouseInput
        {
            get { return Input.mousePosition; }
        }

        private float ScrollWheel
        {
            get { return Input.GetAxis(zoomingAxis); }
        }

        private Vector2 MouseAxis
        {
            get { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        }

        private int ZoomDirection
        {
            get
            {
                bool zoomIn = Input.GetKey(zoomInKey);
                bool zoomOut = Input.GetKey(zoomOutKey);
                if (zoomIn && zoomOut)
                    return 0;
                else if (!zoomIn && zoomOut)
                    return 1;
                else if (zoomIn && !zoomOut)
                    return -1;
                else
                    return 0;
            }
        }

        private int RotationDirection
        {
            get
            {
                bool rotateRight = Input.GetKey(rotateRightKey);
                bool rotateLeft = Input.GetKey(rotateLeftKey);
                if (rotateLeft && rotateRight)
                    return 0;
                else if (rotateLeft && !rotateRight)
                    return -1;
                else if (!rotateLeft && rotateRight)
                    return 1;
                else
                    return 0;
            }
        }

        #endregion

        #region Unity_Methods

        private void Start()
        {
            m_Transform = transform;
        }

        private void Update()
        {
            if (!useFixedUpdate)
                CameraUpdate();
        }

        private void FixedUpdate()
        {
            if (useFixedUpdate)
                CameraUpdate();
        }

        #endregion

        #region RTSCamera_Methods

        /// <summary>
        /// update camera movement and rotation
        /// </summary>
        private void CameraUpdate()
        {
            if (FollowingTarget)
                FollowTarget();
            else
                Move();

            HeightCalculation();
            Rotation();
            LimitPosition();
        }

        /// <summary>
        /// move camera with keyboard or with screen edge
        /// </summary>
        private void Move()
        {
            if (useKeyboardInput)
            {
                Vector3 desiredMove = new Vector3(KeyboardInput.x, 0, KeyboardInput.y);

                desiredMove *= keyboardMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection(desiredMove);

                m_Transform.Translate(desiredMove, Space.Self);
            }

            if (useScreenEdgeInput)
            {
                Vector3 desiredMove = new Vector3();

                Rect leftRect = new Rect(0, 0, screenEdgeBorder, Screen.height);
                Rect rightRect = new Rect(Screen.width - screenEdgeBorder, 0, screenEdgeBorder, Screen.height);
                Rect upRect = new Rect(0, Screen.height - screenEdgeBorder, Screen.width, screenEdgeBorder);
                Rect downRect = new Rect(0, 0, Screen.width, screenEdgeBorder);

                desiredMove.x = leftRect.Contains(MouseInput) ? -1 : rightRect.Contains(MouseInput) ? 1 : 0;
                desiredMove.z = upRect.Contains(MouseInput) ? 1 : downRect.Contains(MouseInput) ? -1 : 0;

                desiredMove *= screenEdgeMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection(desiredMove);

                m_Transform.Translate(desiredMove, Space.Self);
            }

            if (usePanning && Input.GetKey(panningKey) && MouseAxis != Vector2.zero)
            {
                Vector3 desiredMove = new Vector3(-MouseAxis.x, 0, -MouseAxis.y);

                desiredMove *= panningSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection(desiredMove);

                m_Transform.Translate(desiredMove, Space.Self);
            }

            //旋转视角
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0))
            {
                float xAngle = m_Transform.localEulerAngles.x + MouseAxis.y;
                if (xAngle > 90f)
                {
                    xAngle = 90f;
                }
                if (xAngle < 45f)
                {
                    xAngle = 45f;
                }
                m_Transform.localEulerAngles = new Vector3(xAngle, m_Transform.localEulerAngles.y, m_Transform.localEulerAngles.z);
            }
        }

        /// <summary>
        /// calcualte height
        /// </summary>
        private void HeightCalculation()
        {
            float distanceToGround = DistanceToGround();

            if (initWithOriginHeight && originHeightSetted == false)
            {
                if (distanceToGround > maxHeight)
                {
                    distanceToGround = maxHeight;
                }
                if (distanceToGround < minHeight)
                {
                    distanceToGround = minHeight;
                }
                zoomPos = (distanceToGround - minHeight) / (maxHeight - minHeight);
                //Debug.Log("distanceToGround:" + distanceToGround + ", zoomPos:" + zoomPos);
                originHeightSetted = true;
                return;
            }


            if (useScrollwheelZooming)
                zoomPos += ScrollWheel * Time.deltaTime * scrollWheelZoomingSensitivity;
            if (useKeyboardZooming)
                zoomPos += ZoomDirection * Time.deltaTime * keyboardZoomingSensitivity;

            zoomPos = Mathf.Clamp01(zoomPos);

            float targetHeight = Mathf.Lerp(minHeight, maxHeight, zoomPos);
            float difference = 0;

            if (distanceToGround != targetHeight)
                difference = targetHeight - distanceToGround;


            m_Transform.position = Vector3.Lerp(m_Transform.position,
                new Vector3(m_Transform.position.x, targetHeight + difference, m_Transform.position.z), Time.deltaTime * heightDampening);
        }

        /// <summary>
        /// rotate camera
        /// </summary>
        private void Rotation()
        {
            if (useKeyboardRotation)
                transform.Rotate(Vector3.up, RotationDirection * Time.deltaTime * rotationSped, Space.World);

            if (useMouseRotation && Input.GetKey(mouseRotationKey))
                m_Transform.Rotate(Vector3.up, -MouseAxis.x * Time.deltaTime * mouseRotationSpeed, Space.World);
        }

        /// <summary>
        /// follow target if target != null
        /// 这种FollowTarget实质为:设置相机x,z坐标改成目标坐标，同时附加设置targetOffset。
        /// 这种效果不是太理想，特别是相机处于旋转状态下，设置FollowTarget后，相机的视野中心并不是目标物体。
        /// 较理想的是根据目标物体位置和当前相机信息反算出相机的目标位置
        /// </summary>
        // private void FollowTarget()
        //{
        //    Vector3 targetPos = new Vector3(targetFollow.position.x, m_Transform.position.y, targetFollow.position.z) + targetOffset;
        //    m_Transform.position = Vector3.MoveTowards(m_Transform.position, targetPos, Time.deltaTime * followingSpeed);
        //}
        private void FollowTarget()
        { 
            var hitPos = GetZRaycastHitGroundPos();
            hitPos.y = targetFollow.position.y;

            Vector3 moveVec = targetFollow.position - hitPos;
            Vector3 targetPos = m_Transform.position + moveVec;
            float dis = Vector3.Distance(targetPos, m_Transform.position);
            //Debug.Log("dis:" + dis);
            m_Transform.position = Vector3.MoveTowards(m_Transform.position, targetPos, Time.deltaTime * followingSpeed);
        }
 
        /// <summary>
        /// limit camera position
        /// </summary>
        private void LimitPosition()
        {
            if (!limitMap)
                return;

            m_Transform.position = new Vector3(Mathf.Clamp(m_Transform.position.x, limitXMin, limitXMax),
                m_Transform.position.y,
                Mathf.Clamp(m_Transform.position.z, limitYMin, limitYMax));
        }

        /// <summary>
        /// set the target
        /// </summary>
        /// <param name="target"></param>
        public void SetTarget(Transform target)
        {
            targetFollow = target;
        }

        /// <summary>
        /// reset the target (target is set to null)
        /// </summary>
        public void ResetTarget()
        {
            targetFollow = null;
        }

        /// <summary>
        /// calculate distance to ground
        /// </summary>
        /// <returns></returns>
        private float DistanceToGround()
        {
            Ray ray = new Ray(m_Transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, groundMask.value))
                return (hit.point - m_Transform.position).magnitude;


            return 0f;
        }



        /// <summary>
        /// Get obj's z direction collider with ground's pos
        /// </summary>
        /// <returns></returns>
        private Vector3 GetZRaycastHitGroundPos()
        {
            Ray ray = new Ray(m_Transform.position, m_Transform.forward);
            RaycastHit hit;
            //Debug.Log("groundMask .value" + groundMask.value + " ,xxx:" + (1 << 6));

            if (Physics.Raycast(ray, out hit, 100, groundMask.value))
                return hit.point;

            return Vector3.zero;
        }

        #endregion

        #region Inspector Ext
        [ContextMenu("CurHeight")]
        void DoLogHeight()
        {
            Debug.Log("height:" + DistanceToGround());
        }
        #endregion
    }
}