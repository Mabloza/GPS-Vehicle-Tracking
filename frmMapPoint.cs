using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SMS
{
    public partial class frmMapPoint : Form
    {
        public bool GetLocation;
        bool MeasureDistance;
        public MapPoint.Location TempLocation;
        public Form1 mainFrm;
        MapPoint.Location point1, point2;
        bool p1;
        MapPoint.Shape linea;

        public frmMapPoint()
        {
            InitializeComponent();
        }

        public void loadMap()
        {
            try
            {
                Map.NewMap(MapPoint.GeoMapRegion.geoMapEurope);
                Map.ActiveMap.GoToLatLong(42.27160, 13.03081, Map.ActiveMap.Altitude);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, "Impossibile inizializzare la mappa. Contattare il supporto tecnico oppure provare a reinstallare il prodotto.\r\nErrore:"+ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmMapPoint_Load(object sender, EventArgs e)
        {

        }

        private void frmMapPoint_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Map.ActiveMap != null)
            {
                Map.SaveMap();
                Map.CloseMap();
                Map.Dispose();
            }
        }

        private void Map_BeforeClick(object sender, AxMapPoint._IMappointCtrlEvents_BeforeClickEvent e)
        {
            if (MeasureDistance)
            {
                if (!p1)
                {
                    point1 = Map.ActiveMap.XYToLocation(e.x, e.y);
                    p1 = true;
                }
                else
                {
                    point2 = Map.ActiveMap.XYToLocation(e.x, e.y);
                    Map.ActiveMap.Shapes.AddShape(MapPoint.GeoAutoShapeType.geoShapeOval, point2, 100, 100);
                    Map.ActiveMap.Shapes.AddLine(point1, point2);
                    double distance = Map.ActiveMap.Distance(point1, point2);
                    MessageBox.Show("Distanza rilevata: " + Math.Round(distance, 2) + " Km");
                    MeasureDistance = false;
                    Map.MousePointer = MapPoint.GeoPointer.geoPointerArrow;
                    p1 = false;
                }

            }
        }

        private void Map_BeforeDblClick(object sender, AxMapPoint._IMappointCtrlEvents_BeforeDblClickEvent e)
        {
            if (GetLocation)
            {
                TempLocation = this.Map.ActiveMap.XYToLocation(e.x, e.y);
                GetLocation = true;
                mainFrm.frm_obiettivo.setNewLocation(TempLocation);
            }
        }

        private void syradeEDatiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.MapStyle = MapPoint.GeoMapStyle.geoMapStyleRoadData;
        }

        private void datiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.MapStyle = MapPoint.GeoMapStyle.geoMapStyleData;
        }

        private void notteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.MapStyle = MapPoint.GeoMapStyle.geoMapStyleNight;
        }

        private void politicaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.MapStyle = MapPoint.GeoMapStyle.geoMapStylePolitical;
        }

        private void sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.MapStyle = MapPoint.GeoMapStyle.geoMapStyleRoad;
        }

        private void terrenoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.MapStyle = MapPoint.GeoMapStyle.geoMapStyleTerrain;
        }

        private void viasualizzaOnlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Map.ActiveMap.ShowInOnlineMap();
        }

        private void direzioneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Map.ActiveMap.SavedWebPages.Add(saveFileDialog1.FileName, Map.ActiveMap.Location, "Mappa", false, false, false, 800, 600, true, true, false, false);
            }
        }

        private void mappaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Map.ActiveMap.SavedWebPages.Add(saveFileDialog1.FileName, Map.ActiveMap.Location, "Mappa", true, false, false, 800, 600, false, true, false, false);
            }
        }

        private void mappaEDirezioneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Map.ActiveMap.SavedWebPages.Add(saveFileDialog1.FileName, Map.ActiveMap.Location, "Mappa", true, false, false, 800, 600, true, true, false, false);
            }
        }

        private void mappaMapPointToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void Map_LocationChanged(object sender, EventArgs e)
        {
            
        }

        private void misuraDistanzaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MeasureDistance = true;
            Map.MousePointer = MapPoint.GeoPointer.geoPointerCrosshair;
        }

        private void Map_MouseMoveEvent(object sender, AxMapPoint._IMappointCtrlEvents_MouseMoveEvent e)
        {
            if (MeasureDistance && p1)
            {
                if (linea!=null)
                    linea.Delete();
                linea = Map.ActiveMap.Shapes.AddLine(point1, Map.ActiveMap.XYToLocation(e.x, e.y));
            }
        }
    }
}
