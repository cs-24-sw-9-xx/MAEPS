// Copyright 2024 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;
using System.Linq;

using Maes.Simulation;
using Maes.Utilities;

using UnityEngine;

// ReSharper disable ConvertIfStatementToNullCoalescingAssignment

namespace Maes.UI
{
    internal class CameraController : MonoBehaviour
    {
        // HACK
        public static CameraController SingletonInstance = null!;
        public Transform? movementTransform;

        // HACK
        private List<CamAssembly> _cams = null!;
        public Camera currentCam = null!; // HACK!

        public GameObject simulationManagerObject = null!;
        private ISimulationManager _simulationManager = null!;

        public float movementSpeed;
        public float movementTime;

        public float rotationAmount;
        // public Vector3 zoomAmount;

        public Vector3 newPosition;

        public Quaternion newRotation;
        // public Vector3 newZoom;

        public Vector3 dragStartPosition;
        public Vector3 dragCurrentPosition;

        public Vector3 rotateStartPosition;
        public Vector3 rotateCurrentPosition;

        public List<UIMovementButton> buttons = null!;

        public List<RectTransform> uiPanels = null!;

        public bool stickyCam;

        // Start is called before the first frame update
        private void Start()
        {
            _simulationManager = simulationManagerObject.GetComponent<ISimulationManager>();
            SingletonInstance = this;
            var t = transform; // Temp storage of build-in is (apparently) more efficient than repeated access.
            newPosition = t.position;
            newRotation = t.rotation;
            CameraInitialization();
            stickyCam = false;
        }

        private void CameraInitialization()
        {
            _cams = new List<CamAssembly>();
            foreach (var c in GetComponentsInChildren<Camera>(includeInactive: true))
            {
                var ct = c.transform;
                _cams.Add(new CamAssembly(ct.localPosition, -1 * ct.up, c));
                c.gameObject.SetActive(false);
            }

            currentCam = _cams.Find(c => c.Camera.name == "Camera90").Camera;
            currentCam.gameObject.SetActive(true);
        }

        // Update is called once per frame
        private void Update()
        {
            var mouseWorldPosition = GetMouseWorldPosition();
            if (mouseWorldPosition != null)
            {
                OnNewMouseWorldPosition(mouseWorldPosition.Value);
            }
            // Update the camera position (either by following a robot or through mouse movement)
            UpdateCameraPosition(mouseWorldPosition);

            HandleKeyboardMovementInput();
            HandleCameraSelect();
            HandleMouseRotateZoomInput();
            HandleKeyboardRotateZoomInput();

            ApplyMovement();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                movementTransform = null;
                // Notify current simulation that no robot is selected
                _simulationManager.CurrentSimulation?.SetSelectedRobot(null);
                _simulationManager.CurrentSimulation?.SetSelectedTag(null);
                _simulationManager.CurrentSimulation?.ClearVisualTags();
            }
        }

        private void HandleCameraSelect()
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                return;
            }

            if (Input.GetKey(KeyCode.Alpha1))
            {
                SwitchCameraTo("Camera45");
            }

            if (Input.GetKey(KeyCode.Alpha2))
            {
                SwitchCameraTo("Camera70");
            }

            if (Input.GetKey(KeyCode.Alpha3))
            {
                SwitchCameraTo("Camera90");
            }
        }

        private void SwitchCameraTo(string camName)
        {
            currentCam.gameObject.SetActive(false);
            currentCam = _cams.Find(c => c.Camera.name == camName).Camera;
            currentCam.gameObject.SetActive(true);
        }

        private void ApplyMovement()
        {
            var t = transform;


            t.position = Vector3.Lerp(t.position, newPosition, Time.deltaTime * movementTime);
            t.rotation = Quaternion.Lerp(t.rotation, newRotation, Time.deltaTime * movementTime);

            foreach (var c in _cams)
            {
                var ct = c.Camera.transform;
                ct.localPosition =
                    Vector3.Lerp(ct.localPosition, c.NewZoom, Time.deltaTime * movementTime);
            }
        }

        private void HandleKeyboardRotateZoomInput()
        {
            if (Input.GetKey(KeyCode.U) || buttons[(int)UIMovementButton.Direction.RLeft].isActive)
            {
                newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
            }

            if (Input.GetKey(KeyCode.O) || buttons[(int)UIMovementButton.Direction.RRight].isActive)
            {
                newRotation *= Quaternion.Euler(Vector3.up * (-1 * rotationAmount));
            }

            if (Input.GetKey(KeyCode.Period) || Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus) ||
                buttons[(int)UIMovementButton.Direction.In].isActive)
            {
                PrepareZoom(1f);
            }

            if (Input.GetKey(KeyCode.Comma) || Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) ||
                buttons[(int)UIMovementButton.Direction.Out].isActive)
            {
                PrepareZoom(-1f);
            }
        }

        private void HandleMouseRotateZoomInput()
        {
            #region MouseZoomRegion

            if (Input.mouseScrollDelta.y != 0 && !uiPanels.Any(panel =>
                    RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition)))
            {
                PrepareZoom(Input.mouseScrollDelta.y);
            }

            #endregion

            #region MouseRotateRegion

            if (Input.GetMouseButtonDown(1))
            {
                rotateStartPosition = Input.mousePosition;
            }

            if (!Input.GetMouseButton(1))
            {
                return;
            }

            rotateCurrentPosition = Input.mousePosition;

            var diff = rotateStartPosition - rotateCurrentPosition;

            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-1 * diff.x / 5f));

            #endregion
        }

        // Positive direction = zoom in
        // Negative direction = zoom out
        private void PrepareZoom(float direction)
        {
            foreach (var cam in _cams)
            {
                if (cam.Camera.orthographic)
                {
                    if (cam.Camera.orthographicSize - cam.ZoomAmount.magnitude * direction > 0)
                    {
                        cam.Camera.orthographicSize -= cam.ZoomAmount.magnitude * direction;
                    }
                }
                else
                {
                    cam.NewZoom += direction * cam.ZoomAmount;
                }
            }
        }

        public void Subscribe(UIMovementButton button)
        {
            buttons.Add(button);
            buttons = buttons.OrderBy(b => b.direction).ToList();
        }

        public void Subscribe(RectTransform panel)
        {
            uiPanels.Add(panel);
        }

        private Vector2? GetMouseWorldPosition()
        {
            // Create temp plane along playing field, and a the current mouse position
            var plane = new Plane(Vector3.forward, Vector3.zero);
            var ray = currentCam.ScreenPointToRay(Input.mousePosition);


            // Only continue if the ray cast intersects the plane
            if (!plane.Raycast(ray, out var entry))
            {
                return null;
            }

            var mouseWorldPosition = ray.GetPoint(entry);
            return mouseWorldPosition;
        }

        private void OnNewMouseWorldPosition(Vector2 mouseWorldPosition)
        {
            // Update the UI to show the current position of the mouse in world space
            // (The frame of reference changes between ros and maes mode)
            if (GlobalSettings.IsRosMode)
            {
                _simulationManager.SimulationInfoUIController.UpdateMouseCoordinates(Geometry.ToRosCoord(mouseWorldPosition));
            }
            else if (_simulationManager.CurrentSimulation != null)
            {
                // var coord = SimulationManager.CurrentSimulation.WorldCoordinateToSlamCoordinate(mouseWorldPosition);
                _simulationManager.SimulationInfoUIController.UpdateMouseCoordinates(mouseWorldPosition);
            }
        }

        private void UpdateCameraPosition(Vector2? mouseWorldPosition)
        {
            // If sticky cam is enabled and a robot is selected, then camera movement is determined entirely by the
            // movement of the robot
            if (stickyCam && movementTransform is not null)
            {
                newPosition = movementTransform.position;
                return;
            }

            // Only use mouse for camera control if the mouse is within the world bounds (ie. it is not hovering over UI)
            if (mouseWorldPosition == null)
            {
                return;
            }

            if (uiPanels.Any(panel =>
                RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition)))
            {
                return; // Don't do anything here, if mouse is in a UI panel.
            }

            #region MouseMovementRegion

            // If left mouse button has been clicked since last update()
            if (Input.GetMouseButtonDown(0))
            {
                dragStartPosition = mouseWorldPosition.Value;
            }

            // If left mouse button is still being held down since last update()
            if (Input.GetMouseButton(0))
            {
                dragCurrentPosition = mouseWorldPosition.Value;
                // New position should be current position, plus difference in dragged position, relative to temp plane
                newPosition = transform.position + (dragStartPosition - dragCurrentPosition);
            }

            #endregion
        }

        private void HandleKeyboardMovementInput()
        {
            var t = transform;
            if (Input.GetKey(KeyCode.I) || buttons[(int)UIMovementButton.Direction.Forwards].isActive)
            {
                newPosition += t.forward * movementSpeed;
            }

            if (Input.GetKey(KeyCode.K) || buttons[(int)UIMovementButton.Direction.Backwards].isActive)
            {
                newPosition += t.forward * (-1 * movementSpeed);
            }

            if (Input.GetKey(KeyCode.J) || buttons[(int)UIMovementButton.Direction.Left].isActive)
            {
                newPosition += t.right * (-1 * movementSpeed);
            }

            if (Input.GetKey(KeyCode.L) || buttons[(int)UIMovementButton.Direction.Right].isActive)
            {
                newPosition += t.right * movementSpeed;
            }
        }

        private class CamAssembly
        {
            public Vector3 NewZoom;
            public readonly Vector3 ZoomAmount;
            public readonly Camera Camera;

            public CamAssembly(Vector3 newZoom, Vector3 zoomAmount, Camera camera)
            {
                NewZoom = newZoom;
                ZoomAmount = zoomAmount;
                Camera = camera;
            }
        }
    }
}