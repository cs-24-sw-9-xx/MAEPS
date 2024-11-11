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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Simulation;

using UnityEngine;

namespace Maes
{
    public class FogOfWarScript : MonoBehaviour
    {
        public ISimulationManager simulationManager = null!;
        public GameObject _fogOfWarPlane = null!;
        public List<Transform> robots = null!;
        public LayerMask _foglayer;
        private float _revealRadius;
        private float _revealRadiusSqr;

        // Set by Start
        private Mesh _mesh = null!;
        private Vector3[] _vertices = null!;
        private Color[] _colors = null!;

        // Use this for initialization
        private void Start()
        {
            foreach (var robot in simulationManager.CurrentSimulation!.Robots)
            {
                robots.Add(robot.transform);
            }

            _revealRadius = simulationManager.CurrentScenario!.RobotConstraints.SlamRayTraceRange;
            _revealRadiusSqr = _revealRadius * _revealRadius;
            _mesh = _fogOfWarPlane.GetComponent<MeshFilter>().mesh;
            _vertices = _mesh.vertices;
            _colors = new Color[_vertices.Length];
            for (var i = 0; i < _colors.Length; i++)
            {
                _colors[i] = Color.black;
            }
            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i] = _fogOfWarPlane.transform.TransformPoint(_vertices[i]);
            }
            UpdateColor();
        }

        // Update is called once per frame
        private void Update() //various optimizations options, robots shoot up to plane, instead of from camera, visible area around robot shoots up
        {
            foreach (var robot in robots)
            {
                var r = new Ray(transform.position, robot.position - transform.position);
                if (Physics.Raycast(r, out var hit, 1000, _foglayer, QueryTriggerInteraction.Collide))
                {
                    for (var i = 0; i < _vertices.Length; i++)
                    {
                        var v = _vertices[i];
                        var dist = Vector3.SqrMagnitude(v - hit.point);
                        if (dist < _revealRadiusSqr)
                        {
                            var alpha = Mathf.Min(_colors[i].a, dist / _revealRadiusSqr);
                            _colors[i].a = alpha;
                        }
                    }
                    UpdateColor();
                }
            }

        }

        private void UpdateColor()
        {
            _mesh.colors = _colors;
        }
    }
}