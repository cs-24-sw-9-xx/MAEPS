// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map
{
    public class EnvironmentTag
    {

        public readonly int Sender;
        public readonly string Content;
        public readonly Vector3 WorldPosition; // Coord in Unity
        public readonly Vector2 MapPosition; // Coord in tile map

        private readonly GameObject _model;


        public EnvironmentTag(int sender, GameObject model, string content)
        {
            Sender = sender;
            _model = model;
            Content = content;
            WorldPosition = model.transform.position;
            MapPosition = WorldPosition;

            _model.GetComponent<VisibleTagInfoHandler>().SetTag(this);
        }

        public override string ToString()
        {
            return $"| Robot{Sender} |:\n" + Content;
        }

        public void SetVisibility(bool val)
        {
            _model.SetActive(val);
        }

        public string GetDebugInfo()
        {
            var position = GlobalSettings.IsRosMode ? Geometry.ToRosCoord(MapPosition) : MapPosition;

            return $"Tag content:  {Content}\n"
                   + $"Deposited by: Robot{Sender}\n"
                   + $"Position:     ({position.x},{position.y})";
        }
    }
}