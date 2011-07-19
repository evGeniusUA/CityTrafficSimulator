﻿/*
 *  CityTrafficSimulator - a tool to simulate traffic in urban areas and on intersections
 *  Copyright (C) 2005-2010, Christian Schulte zu Berge
 *  
 *  This program is free software; you can redistribute it and/or modify it under the 
 *  terms of the GNU General Public License as published by the Free Software 
 *  Foundation; either version 3 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful, but WITHOUT ANY 
 *  WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A 
 *  PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with this 
 *  program; if not, see <http://www.gnu.org/licenses/>.
 * 
 *  Web:  http://www.cszb.net
 *  Mail: software@cszb.net
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml.Serialization;

using CityTrafficSimulator.Vehicle;


namespace CityTrafficSimulator
    {
	/// <summary>
	/// Knotenpunkt zwischen zwei NodeConnections
	/// </summary>
    [Serializable]
    public class LineNode : ISavable, ITickable, IDrawable
		{
		#region statische Felder zum Zeichnen

		private static Pen blackPen = new Pen(Color.Black);
		private static Brush blackBrush = new SolidBrush(Color.Black);
		private static Brush greenBrush = new SolidBrush(Color.Green);
		private static Brush redBrush = new SolidBrush(Color.Red);

		#endregion

		/// <summary>
		/// zum LineNode gehörige Ampel
		/// </summary>
		[XmlIgnore]
		public TrafficLight tLight;

        #region Positionen der Stützpunkte
        /// <summary>
        /// absolute Position des LineNodes in Weltkoordinaten
        /// </summary>
        private Vector2 m_Position;
		/// <summary>
		/// absolute Position des LineNodes in Weltkoordinaten
		/// </summary>
        public Vector2 position
            {
            get { return m_Position; }
            set { m_Position = value; UpdateNodeConnections(true); UpdateNodeGraphics(); }
            }
		/// <summary>
		/// Quadrat der Kantenlänge 8 um die absolute Position des LineNodes in Weltkoordinaten
		/// </summary>
        public RectangleF positionRect
            {
            get { return new RectangleF((float)position.X - 4, (float)position.Y - 4, 8, 8); }
            }

        
		/// <summary>
		/// Einflugvektor
		/// </summary>
        private Vector2 m_In;
		/// <summary>
		/// Ausflugvektor
		/// </summary>
        private Vector2 m_Out;
		/// <summary>
		/// relativer Einflugvektor
		/// </summary>
        public Vector2 inSlope
            {
            get { return m_In; }
            set { m_In = value; UpdateNodeConnections(true); UpdateNodeGraphics(); }
            }
		/// <summary>
		/// absolute Position des Einflugvektorankers in Weltkoordinaten
		/// </summary>
        [XmlIgnore]
        public Vector2 inSlopeAbs
            {
            get { return position + m_In; }
            set { m_In = value - position; UpdateNodeConnections(true); UpdateNodeGraphics(); }
            }
		/// <summary>
		/// Quadrat der Kantenlänge 8 um die absolute Position des Einflugvektorankers in Weltkoordinaten
		/// </summary>
		public RectangleF inSlopeRect
            {
            get { return new RectangleF((float)inSlopeAbs.X - 4, (float)inSlopeAbs.Y - 4, 8, 8); }
            }

        /// <summary>
        /// relativer Ausflugvektor
        /// </summary>
        public Vector2 outSlope
            {
            get { return m_Out; }
            set { m_Out = value; UpdateNodeConnections(true); UpdateNodeGraphics(); }
            }
		/// <summary>
		/// Quadrat der Kantenlänge 8 um die absolute Position des Ausflugvektorankers in Weltkoordinaten
		/// </summary>
        public RectangleF outSlopeRect
            {
            get { return new RectangleF((float)outSlopeAbs.X - 4, (float)outSlopeAbs.Y - 4, 8, 8); }
            }
		/// <summary>
		/// absolute Position des Ausflugvektorankers in Weltkoordinaten
		/// </summary>
        [XmlIgnore]
		public Vector2 outSlopeAbs
            {
            get { return position + m_Out; }
            set { m_Out = value - position; UpdateNodeConnections(true); UpdateNodeGraphics(); }
            }
        #endregion

        #region Connections
		
		/// <summary>
		/// vorherige NodeConnections
		/// </summary>
        private List<NodeConnection> m_nextConnections = new List<NodeConnection>(); // Vorheriger Knoten
		/// <summary>
		/// nachfolgende NodeConnections
		/// </summary>
        private List<NodeConnection> m_prevConnections = new List<NodeConnection>(); // Nachfolgender Knoten

		/// <summary>
		/// nachfolgende NodeConnections
		/// </summary>
		[XmlIgnore]
		public List<NodeConnection> nextConnections
            {
            get { return m_nextConnections; } 
            }
		/// <summary>
		/// vorherige NodeConnections
		/// </summary>
		[XmlIgnore]
		public List<NodeConnection> prevConnections
            {
            get { return m_prevConnections; } 
            }


		/// <summary>
		/// sucht  die NodeConnection zum LineNode lineNode heraus
		/// </summary>
		/// <param name="lineNode">zu suchender LineNode</param>
		/// <returns>erstbeste NodeConnection in nextConnections mit (nc.endNode == lineNode) oder null</returns>
        public NodeConnection GetNodeConnectionTo(LineNode lineNode)
            {
            foreach (NodeConnection lc in m_nextConnections)
                {
                if (lc.endNode == lineNode)
                    {
                    return lc;
                    }
                }
            return null;
            }

        /// <summary>
        /// Aktualisiert sämtliche NodeConnections in nextNodes
        /// </summary>
		/// <param name="doRecursive">soll die Aktualisierung auch bei den prevNodes durchgeführt werden</param>
        public void UpdateNodeConnections(bool doRecursive)
            {
            foreach (NodeConnection lc in m_nextConnections)
                {
                lc.lineSegment = null;
                lc.lineSegment = new LineSegment(0, this.position, this.outSlopeAbs, lc.endNode.inSlopeAbs, lc.endNode.position);
                }
            if (doRecursive)
                {
                foreach (NodeConnection lc in m_prevConnections)
                    {
                    lc.startNode.UpdateNodeConnections(false);
                    }
                }
            }
        #endregion

        #region Konstruktoren
		/// <summary>
		/// Standardkonstruktor, wird nur für die XML Serialisierung benötigt.
		/// NICHT VERWENDEN, dieser Konstruktor vergibt keine neuen Hashcodes!
		/// </summary>
        public LineNode()
            {
            this.position = new Vector2(0, 0);
            this.inSlope = new Vector2(0, 0);
            this.outSlope = new Vector2(0, 0);
			
			hashcode = hashcodeIndex++;
            }

		/// <summary>
		/// Konstruktor, erstell einen neuen LineNode an der Position position.
		/// in-/outSlope werden mit (0,0) initialisiert.
		/// </summary>
		/// <param name="position">absolute Position</param>
        public LineNode(Vector2 position)
            {
            this.position = position;
            this.inSlope = new Vector2(0, 0);
            this.outSlope = new Vector2(0, 0);

			// Hashcode vergeben
			hashcode = hashcodeIndex++;
            }

		/// <summary>
		/// Konstruktor, erstell einen neuen LineNode an der Position position.
		/// </summary>
		/// <param name="Position">absolute Position</param>
		/// <param name="inSlope">eingehender Richtungsvektor</param>
		/// <param name="outSlope">ausgehender Richtungsvektor</param>
        public LineNode(Vector2 Position, Vector2 inSlope, Vector2 outSlope)
            {
            this.position = Position;
            this.inSlope = inSlope;
			this.outSlope = outSlope;

			// Hashcode vergeben
			hashcode = hashcodeIndex++;
            }

        #endregion

		#region Graphics
		/// <summary>
		/// ein GraphicsPath, der die wichtigen Grafiken des Knotens Enthält (Ankerpunkte etc)
		/// </summary>
        private System.Drawing.Drawing2D.GraphicsPath[] m_NodeGraphics = new System.Drawing.Drawing2D.GraphicsPath[4];
		/// <summary>
		/// ein GraphicsPath, der die wichtigen Grafiken des Knotens Enthält (Ankerpunkte etc)
		/// </summary>
        public System.Drawing.Drawing2D.GraphicsPath[] nodeGraphics
            {
            get { return m_NodeGraphics; }
            }

        /// <summary>
        /// aktualisiert das Feld nodeGraphics
        /// </summary>
        private void UpdateNodeGraphics()
            {
            if ((position != null) && (inSlope != null) && (outSlope != null))
                {
                // Linien zu den Stützpunkten
                m_NodeGraphics[0] = new System.Drawing.Drawing2D.GraphicsPath(new PointF[] { inSlopeAbs, position }, new byte[] { 1, 1 });
                m_NodeGraphics[1] = new System.Drawing.Drawing2D.GraphicsPath(new PointF[] { outSlopeAbs, position }, new byte[] { 1, 1 });

                // Stützpunkte
                System.Drawing.Drawing2D.GraphicsPath inPoint = new System.Drawing.Drawing2D.GraphicsPath();
                inPoint.AddEllipse(inSlopeRect);
                m_NodeGraphics[2] = inPoint;

                System.Drawing.Drawing2D.GraphicsPath outPoint = new System.Drawing.Drawing2D.GraphicsPath();
				// wir versuchen ein Dreieck zu zeichnen *lol*
				Vector2 dir = outSlope.Normalized;
				outPoint.AddPolygon(
					new PointF[] { 
						(6*dir) + outSlopeAbs, 
						(6*dir.RotateCounterClockwise(Math.PI * 2 / 3)) + outSlopeAbs,
						(6*dir.RotateCounterClockwise(Math.PI * 4 / 3)) + outSlopeAbs,
						(6*dir) + outSlopeAbs
					});
                m_NodeGraphics[3] = outPoint;
                }
            }

        #endregion

        #region Autos
		/// <summary>
		/// Gibt die Anzahl der Autos auf den einhenden NodeConnections zurück
		/// </summary>
		/// <returns>Anzahl der Autos auf den einhenden NodeConnections</returns>
        public int GetCountOfLastVehiclesBefore()
            {
            int count = 0;
            foreach (NodeConnection nc in m_prevConnections)
                {
                if (nc.vehicles.Count != 0)
                    {
                    count++;
                    }
                }
            return count;
            }

        #endregion

		#region ITickable Member

		/// <summary>
		/// lässt die Zeit um einen Tick voranschreiten. Reicht Tick() an alle IVehicles auf allen ausgehenden NodeConnections weiter.
		/// </summary>
		/// <param name="tickLength">Länge eines Ticks in Sekunden (berechnet sich mit 1/#Ticks pro Sekunde)</param>
		/// <param name="currentTime">aktuelle Zeit in Sekunden nach Sekunde 0</param>
		public void Tick(double tickLength, double currentTime)
            {
            foreach (NodeConnection nc in this.m_nextConnections)
                {
                foreach (IVehicle v in nc.vehicles)
                    {
                    v.Think(tickLength, currentTime);
                    }
                }
			foreach (NodeConnection nc in this.m_nextConnections)
				{
				nc.RemoveAllVehiclesInRemoveList();

				foreach (IVehicle v in nc.vehicles)
					{
					v.Move(tickLength, (float) currentTime);
					}

				nc.RemoveAllVehiclesInRemoveList();
				}
			}

		/// <summary>
		/// sagt dem Objekt Bescheid, dass der Tick vorbei ist. Reicht Reset() an alle IVehicles auf allen ausgehenden NodeConnections weiter.
		/// (wir zur Zeit nur von IVehicle benötigt)
		/// </summary>
		public void Reset()
			{
			foreach (NodeConnection nc in this.m_nextConnections)
				{
				foreach (IVehicle v in nc.vehicles)
					{
					v.Reset();
					}
				}
			}

		#endregion

		#region Speichern/ Laden
		/// <summary>
		/// Bereite alles fürs Speichern vor
		/// Hashes berechnen, etc.
		/// </summary>
		public void PrepareForSave()
            {
			// TODO: ist das hier wirklich nötig? macht das nicht die NodeSteuerung?
            foreach (NodeConnection nc in m_nextConnections)
                nc.PrepareForSave();
            foreach (NodeConnection nc in m_prevConnections)
                nc.PrepareForSave();

			if (tLight != null)
				tLight.PrepareForSave();

			}

		/// <summary>
		/// Stelle den ursprünglich gespeicherten Zustand wieder her
		/// zum Beispiel durch die Hashes
		/// </summary>
		/// <param name="saveVersion">Version der gespeicherten Datei</param>
		/// <param name="nodesList">Liste der bereits wiederhergestellten LineNodes</param>
		public void RecoverFromLoad(int saveVersion, List<LineNode> nodesList)
            {
			// Klassenvariable für Hashcode erhöhen um Kollisionen für zukünftige LineNodes zu verhindern
			if (hashcodeIndex <= hashcode)
				{
				hashcodeIndex = hashcode + 1;
				}

            UpdateNodeConnections(false);
            UpdateNodeGraphics();

			if (tLight != null)
				tLight.RecoverFromLoad(saveVersion, nodesList);

			}
        #endregion


		#region Hashcodes

		/*
		 * Nachdem der ursprüngliche Ansatz zu Hashen zu argen Kollisionen geführt hat, nun eine verlässliche Methode für Kollisionsfreie Hashes 
		 * mittels eindeutiger IDs für jeden LineNode die über statisch Klassenvariablen vergeben werden
		 */

		/// <summary>
		/// Klassenvariable welche den letzten vergebenen hashcode speichert und bei jeder Instanziierung eines Objektes inkrementiert werden muss
		/// </summary>
		private static int hashcodeIndex = 0;

		/// <summary>
		/// Hashcode des instanziierten Objektes
		/// </summary>
		public int hashcode = -1;

		/// <summary>
		/// gibt den Hashcode des Fahrzeuges zurück.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
			{
			return hashcode;
			}

		/// <summary>
		/// Setzt die statische Klassenvariable hashcodeIndex zurück. Achtung: darf nur in bestimmten Fällen aufgerufen werden.
		/// </summary>
		public static void ResetHashcodeIndex()
			{
			hashcodeIndex = 0;
			}

		#endregion

		/// <summary>
		/// ToString() Methode des Knotens
		/// </summary>
		/// <returns>#hashcode @ [ position.ToString() ]</returns>
		public override string ToString()
            {
            return "#" + hashcode.ToString() + " @ [" + position.ToString() + "]";
            }

		/// <summary>
		/// Kapselt untersuchte Knoten des A*-Algorithmus' zusammen mit ihren Vorgängerknoten in einer einfach verketteten Liste
		/// </summary>
        public class LinkedLineNode
            {
			/// <summary>
			/// der untersuchte Knoten
			/// </summary>
            public LineNode node;

			/// <summary>
			/// Vorgängerknoten
			/// </summary>
			private LineNode.LinkedLineNode m_parent;
			/// <summary>
			/// Vorgängerknoten
			/// </summary>
			public LineNode.LinkedLineNode parent
				{
				get { return m_parent; }
				set 
					{ 
					m_parent = value;
					if (m_parent == null)
						{
						countOfParents = 0;
						}
					else
						{
						countOfParents = m_parent.countOfParents + 1;
						}
					}
				}

			/// <summary>
			/// Flag, ob ein Spurwechsel zum erreichen dieses Knotens nötig ist
			/// </summary>
			public bool lineChangeNeeded { get; private set; }

			/// <summary>
			/// Anzahl der Elternknoten
			/// </summary>
			public int countOfParents { get; private set; }

			/// <summary>
			/// legt einen neuen LinkedLineNode an
			/// </summary>
			/// <param name="node">der untersuchte Knoten</param>
			/// <param name="parent">Vorgängerknoten</param>
			/// <param name="lineChangeNeeded">Flag, ob ein Spurwechsel zum erreichen dieses Knotens nötig ist</param>
            public LinkedLineNode(LineNode node, LineNode.LinkedLineNode parent, bool lineChangeNeeded)
                {
                this.node = node;
                this.parent = parent;
				this.lineChangeNeeded = lineChangeNeeded;
				if (parent == null)
					{
					countOfParents = 0;
					}
				else
					{
					countOfParents = parent.countOfParents + 1;
					}
                }

			/// <summary>
			/// gibt die summierte Länge des Knotens und aller seiner Vorgängerknoten zurück
			/// </summary>
			/// <returns></returns>
            public double GetLength()
                {
				double length = 0;
				if (parent != null)
					{
					if (!lineChangeNeeded)
						{
						length = parent.node.GetNodeConnectionTo(node).lineSegment.length;
						}
					else
						{
						// TODO: sinnvolle Länge berechnen
						}
					}
				

				return (parent == null) ? length : length + parent.GetLength();
                }
            }

		#region IDrawable Member

		/// <summary>
		/// Zeichnet das Vehicle auf der Zeichenfläche g
		/// </summary>
		/// <param name="g">Die Zeichenfläche auf der gezeichnet werden soll</param>
		public void Draw(Graphics g)
			{
			// Node malen
			g.DrawRectangle(blackPen, positionRect.X, positionRect.Y, positionRect.Width, positionRect.Height);

			if (tLight != null)
				{
				switch (tLight.trafficLightState)
					{
				case TrafficLight.State.GREEN:
					g.FillRectangle(greenBrush, positionRect);
					break;
				case TrafficLight.State.RED:
					g.FillRectangle(redBrush, positionRect);
					break;
					}
				}
			}

		/// <summary>
		/// Zeichnet Debuginformationen auf die Zeichenfläche g
		/// </summary>
		/// <param name="g">Die Zeichenfläche auf der gezeichnet werden soll</param>
		public void DrawDebugData(Graphics g)
			{
			g.DrawString(hashcode.ToString(), new Font("Arial", 8), blackBrush, position);
			}

		#endregion
		}
    }