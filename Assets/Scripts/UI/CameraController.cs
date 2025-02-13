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

using Maes.Simulation;
using Maes.Utilities;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Maes.UI
{
    public class CameraController : MonoBehaviour
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

        public bool stickyCam;

        public UIDocument uiDocument = null!;
        public UIDocument modeSpecificUiDocument = null!;

        private readonly Dictionary<Direction, bool> _buttonStates = new();

        private Button _leftButton = null!;
        private Button _rightButton = null!;
        private Button _upButton = null!;
        private Button _downButton = null!;

        private Button _rotateCounterClockwiseButton = null!;
        private Button _rotateClockwiseButton = null!;
        private Button _zoomInButton = null!;
        private Button _zoomOutButton = null!;


        // Start is called before the first frame update
        private void Start()
        {
            _simulationManager = simulationManagerObject.GetComponent<ISimulationManager>();

            _leftButton = uiDocument.rootVisualElement.Q<Button>("MoveLeftButton");
            _rightButton = uiDocument.rootVisualElement.Q<Button>("MoveRightButton");
            _upButton = uiDocument.rootVisualElement.Q<Button>("MoveUpButton");
            _downButton = uiDocument.rootVisualElement.Q<Button>("MoveDownButton");

            _rotateCounterClockwiseButton = uiDocument.rootVisualElement.Q<Button>("RotateCounterClockwiseButton");
            _rotateClockwiseButton = uiDocument.rootVisualElement.Q<Button>("RotateClockwiseButton");
            _zoomInButton = uiDocument.rootVisualElement.Q<Button>("ZoomInButton");
            _zoomOutButton = uiDocument.rootVisualElement.Q<Button>("ZoomOutButton");

            HandleButton(_leftButton, Direction.Left);
            HandleButton(_rightButton, Direction.Right);
            HandleButton(_upButton, Direction.Up);
            HandleButton(_downButton, Direction.Down);

            HandleButton(_rotateCounterClockwiseButton, Direction.RotateCounterClockwise);
            HandleButton(_rotateClockwiseButton, Direction.RotateClockwise);
            HandleButton(_zoomInButton, Direction.ZoomIn);
            HandleButton(_zoomOutButton, Direction.ZoomOut);

            SingletonInstance = this;
            newPosition = transform.position;
            newRotation = transform.rotation;
            CameraInitialization();
            stickyCam = false;
        }

        private void HandleButton(Button button, Direction direction)
        {
            button.RegisterCallback<MouseDownEvent, Direction>(ButtonDownHandler, direction);
            button.RegisterCallback<MouseUpEvent, Direction>(ButtonUpHandler, direction);
        }

        private void ButtonDownHandler(MouseDownEvent mouseDownEvent, Direction direction)
        {
            _buttonStates[direction] = true;
        }

        private void ButtonUpHandler(MouseUpEvent mouseupEvent, Direction direction)
        {
            _buttonStates[direction] = false;
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

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                movementTransform = null;
                // Notify current simulation that no robot is selected
                _simulationManager.CurrentSimulation?.SetSelectedRobot(null);
                _simulationManager.CurrentSimulation?.SetSelectedTag(null);
                if (_simulationManager.CurrentSimulation is PatrollingSimulation patrollingSimulation)
                {
                    patrollingSimulation.SetSelectedVertex(null);
                }
                _simulationManager.CurrentSimulation?.ClearVisualTags();
            }
        }

        private void HandleCameraSelect()
        {
            var keyboard = Keyboard.current;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SwitchCameraTo("Camera45");
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SwitchCameraTo("Camera70");
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
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

        private bool GetButtonState(Direction direction)
        {
            return _buttonStates.TryGetValue(direction, out var value) && value;
        }

        private void HandleKeyboardRotateZoomInput()
        {
            var keyboard = Keyboard.current;

            if (keyboard.uKey.isPressed || GetButtonState(Direction.RotateCounterClockwise))
            {
                newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
            }

            if (keyboard.oKey.isPressed || GetButtonState(Direction.RotateClockwise))
            {
                newRotation *= Quaternion.Euler(Vector3.up * (-1 * rotationAmount));
            }

            if (keyboard.periodKey.isPressed || keyboard.numpadPlusKey.isPressed ||
                GetButtonState(Direction.ZoomIn))
            {
                PrepareZoom(1f);
            }

            if (keyboard.commaKey.isPressed || keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed ||
                GetButtonState(Direction.ZoomOut))
            {
                PrepareZoom(-1f);
            }
        }

        private void HandleMouseRotateZoomInput()
        {
            var mouse = Mouse.current;
            var yScroll = mouse.scroll.y.ReadValue();
            if (yScroll != 0 && !MouseIsOnUI())
            {
                PrepareZoom(yScroll);
            }

            var rightMouseButton = mouse.rightButton;
            if (rightMouseButton.wasPressedThisFrame)
            {
                rotateStartPosition = mouse.position.ReadValue();
            }

            if (!rightMouseButton.isPressed)
            {
                return;
            }

            rotateCurrentPosition = mouse.position.ReadValue();

            var diff = rotateStartPosition - rotateCurrentPosition;

            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-1 * diff.x / 5f));
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

        private Vector2? GetMouseWorldPosition()
        {
            // Create temp plane along playing field, and a the current mouse position
            var plane = new Plane(Vector3.forward, Vector3.zero);
            var ray = currentCam.ScreenPointToRay(Pointer.current.position.ReadValue());


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
            if (stickyCam && movementTransform != null)
            {
                newPosition = movementTransform.position;
                return;
            }

            // Only use mouse for camera control if the mouse is within the world bounds (ie. it is not hovering over UI)
            if (mouseWorldPosition == null)
            {
                return;
            }

            if (MouseIsOnUI())
            {
                return; // Don't do anything here, if mouse is in a UI panel.
            }

            var mouseLeftButton = Mouse.current.leftButton;

            // If left mouse button has been clicked since last update()
            if (mouseLeftButton.wasPressedThisFrame)
            {
                dragStartPosition = mouseWorldPosition.Value;
            }

            // If left mouse button is still being held down since last update()
            if (mouseLeftButton.isPressed)
            {
                dragCurrentPosition = mouseWorldPosition.Value;
                // New position should be current position, plus difference in dragged position, relative to temp plane
                newPosition = transform.position + (dragStartPosition - dragCurrentPosition);
            }
        }

        private void HandleKeyboardMovementInput()
        {
            var keyboard = Keyboard.current;

            var t = transform;
            if (keyboard.iKey.isPressed || GetButtonState(Direction.Up))
            {
                newPosition += t.forward * movementSpeed;
            }

            if (keyboard.kKey.isPressed || GetButtonState(Direction.Down))
            {
                newPosition += t.forward * (-1 * movementSpeed);
            }

            if (keyboard.jKey.isPressed || GetButtonState(Direction.Left))
            {
                newPosition += t.right * (-1 * movementSpeed);
            }

            if (keyboard.lKey.isPressed || GetButtonState(Direction.Right))
            {
                newPosition += t.right * movementSpeed;
            }
        }

        private bool MouseIsOnUI()
        {
            var mousePosition = Pointer.current.position.ReadValue();
            var uiPosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            return !(uiDocument.rootVisualElement.panel.Pick(uiPosition) == null && modeSpecificUiDocument.rootVisualElement.panel.Pick(uiPosition) == null);
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

        private enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            ZoomIn,
            ZoomOut,
            RotateCounterClockwise,
            RotateClockwise
        }
    }
}