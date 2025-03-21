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

using System;
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Simulation;
using Maes.UI;

using QuickOutline;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Maes.Robot
{
    public class MonaRobot : MonoBehaviour, ISimulationUnit, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Transform leftWheelTransform = null!;
        public Transform rightWheelTransform = null!;
        public Outline outLine = null!;
        public int id = -1;

        // Set by Awake
        public ISimulation Simulation { get; private set; } = null!;

        // The controller that provides an interface for moving the robot
        // Set by Awake
        public Robot2DController Controller { get; private set; } = null!;

        public delegate void OnRobotSelectedDelegate(MonaRobot robot);

        public OnRobotSelectedDelegate OnRobotSelected = _ => { };
        public Color32 Color { get; set; }

        public int AssignedPartition { get; set; }

        // The algorithm that controls the logic of the robot
        // Set by RobotSpawner
        public IAlgorithm Algorithm
        {
            get => _algorithm;
            set
            {
                _algorithm = value;

                _preUpdateLogicEnumerator = _algorithm.PreUpdateLogic().GetEnumerator();
                _updateLogicEnumerator = _algorithm.UpdateLogic().GetEnumerator();
            }
        }

        private IAlgorithm _algorithm = null!;

        private IEnumerator<WaitForCondition> _preUpdateLogicEnumerator = null!;
        private IEnumerator<WaitForCondition> _updateLogicEnumerator = null!;

        private int _collidingGameObjects;

        private GameObject _tagPost = null!;

        private Transform _envTagHolder = null!;

        private void Awake()
        {
            _tagPost = Resources.Load<GameObject>("TagPost");
            _envTagHolder = GameObject.Find("EnvTagHolder").transform;
            var rigidBody = GetComponent<Rigidbody2D>();
            Controller = new Robot2DController(rigidBody, transform, leftWheelTransform, rightWheelTransform, this);
            Simulation = GameObject.Find("SimulationManager").GetComponent<ISimulationManager>().CurrentSimulation ?? throw new InvalidOperationException("No current simulation");
        }

        private WaitForCondition _preUpdateLogicWaitForCondition = WaitForCondition.ContinueUpdateLogic();
        private WaitForCondition _updateLogicWaitForCondition = WaitForCondition.ContinueUpdateLogic();

        private int _preUpdateLogicWaitForTicks;
        private int _updateLogicWaitForTicks;


        public void LogicUpdate()
        {
            switch (_preUpdateLogicWaitForCondition.Type)
            {
                case WaitForCondition.ConditionType.LogicTicks:
                    if (--_preUpdateLogicWaitForTicks > 0)
                    {
                        Controller.UpdateLogic();
                        return;
                    }

                    break;
                case WaitForCondition.ConditionType.RobotStatus:
                    if (Controller.GetStatus() != _preUpdateLogicWaitForCondition.RobotStatus)
                    {
                        Controller.UpdateLogic();
                        return;
                    }

                    break;
                case WaitForCondition.ConditionType.ContinueUpdateLogic:
                    // Lets continue :)
                    break;
            }

            _preUpdateLogicEnumerator.MoveNext();
            _preUpdateLogicWaitForCondition = _preUpdateLogicEnumerator.Current;

            // Update state
            switch (_preUpdateLogicWaitForCondition.Type)
            {
                case WaitForCondition.ConditionType.LogicTicks:
                    _preUpdateLogicWaitForTicks = _preUpdateLogicWaitForCondition.LogicTicks;
                    Controller.UpdateLogic();
                    return;
                case WaitForCondition.ConditionType.RobotStatus:
                    Controller.UpdateLogic();
                    return;
                case WaitForCondition.ConditionType.ContinueUpdateLogic:
                    break;
            }

            switch (_updateLogicWaitForCondition.Type)
            {
                case WaitForCondition.ConditionType.LogicTicks:
                    if (--_updateLogicWaitForTicks > 0)
                    {
                        Controller.UpdateLogic();
                        return;
                    }

                    break;
                case WaitForCondition.ConditionType.RobotStatus:
                    if (Controller.GetStatus() != _updateLogicWaitForCondition.RobotStatus)
                    {
                        Controller.UpdateLogic();
                        return;
                    }

                    break;
                case WaitForCondition.ConditionType.ContinueUpdateLogic:
                    // Lets continue :)
                    break;
            }

            _updateLogicEnumerator.MoveNext();
            _updateLogicWaitForCondition = _updateLogicEnumerator.Current;

            // Update state
            switch (_updateLogicWaitForCondition.Type)
            {
                case WaitForCondition.ConditionType.LogicTicks:
                    _updateLogicWaitForTicks = _updateLogicWaitForCondition.LogicTicks;
                    Controller.UpdateLogic();
                    return;
                case WaitForCondition.ConditionType.RobotStatus:
                    Controller.UpdateLogic();
                    return;
                case WaitForCondition.ConditionType.ContinueUpdateLogic:
                    throw new InvalidOperationException("ContinueUpdateLogic is invalid in UpdateLogic");
            }
        }

        public void PhysicsUpdate()
        {
            Controller.UpdateMotorPhysics();
        }

        private void OnCollisionEnter2D(Collision2D _)
        {
            _collidingGameObjects++;

            Controller.NotifyCollided();
        }

        private void OnCollisionExit2D(Collision2D _)
        {
            _collidingGameObjects--;

            if (_collidingGameObjects == 0)
            {
                Controller.NotifyCollisionExit();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tooltip.ShowTooltip_Static($"robot{id}");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideTooltip_Static();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            CameraController.SingletonInstance.movementTransform = transform;
            OnRobotSelected(this);
        }

        public GameObject ClaimTag()
        {
            var gameObj = Instantiate(_tagPost, _envTagHolder);
            gameObj.transform.position = transform.position + new Vector3(0, 0, -0.1f);
            gameObj.SetActive(false);
            gameObj.name = $"robot{id}-{gameObj.name}";
            return gameObj;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = UnityEngine.Color.yellow;
            foreach (var (point, radius) in Controller.DebugCircle)
            {
                Gizmos.DrawWireSphere(point, radius);
            }
        }

        public void DestroyRobot()
        {
            Destroy(gameObject);
        }

        public void ShowOutline(Color? color = null)
        {
            outLine.enabled = true;
            outLine.OutlineColor = color ?? Color;
        }

        public void HideOutline()
        {
            outLine.enabled = false;
        }
    }
}