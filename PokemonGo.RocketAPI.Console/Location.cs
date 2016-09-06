using System;
using System.Windows.Forms;
using GMap.NET.MapProviders;
using System.Net;
using GoogleMapsApi.Entities.Elevation.Request;
using GoogleMapsApi.Entities.Elevation.Response;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET;
using System.Threading.Tasks;
using System.Device.Location;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PokemonGo.RocketAPI.Console
{
    public partial class LocationSelect : Form
    {
        private GMarkerGoogle _botMarker = new GMarkerGoogle(new PointLatLng(), GMarkerGoogleType.red_small);
        private GMapRoute _botRoute = new GMapRoute("BotRoute");
        private GMapOverlay _pokeStopsOverlay = new GMapOverlay("PokeStops");
        private Dictionary<string, GMarkerGoogle> _pokeStopsMarks = new Dictionary<string, GMarkerGoogle>();

        public double alt;
        public bool close = true;

        public LocationSelect(bool asViewOnly)
        {
            InitializeComponent();
            map.Manager.Mode = AccessMode.ServerOnly;

            if (asViewOnly)
                initViewOnly();
        }

        private void initViewOnly()
        {
            //first hide all controls
            foreach (Control c in Controls)
                c.Visible = false;
            //show map
            map.Visible = true;
            map.Dock = DockStyle.Fill;
            map.ShowCenter = false;
            GMapOverlay routeOverlay = new GMapOverlay();
            routeOverlay.Routes.Add(_botRoute);
            routeOverlay.Markers.Add(_botMarker);
            GMarkerGoogle _botStartMarker = new GMarkerGoogle(new PointLatLng(), GMarkerGoogleType.blue_dot);
            _botStartMarker.Position = new PointLatLng(Globals.latitute, Globals.longitude);
            routeOverlay.Markers.Add(_botStartMarker);
            GMapPolygon circle = CreateCircle(new PointLatLng(Globals.latitute, Globals.longitude), Globals.radius, 100);
            routeOverlay.Polygons.Add(circle);
            map.Overlays.Add(routeOverlay);
            map.Overlays.Add(_pokeStopsOverlay);
            //show geodata controls
            label1.Visible = true;
            label2.Visible = true;
            textBox1.Visible = true;
            textBox2.Visible = true;
            cbShowPokeStops.Visible = true;
            //don't ask at closing
            close = false;
            //add & remove live data handler after form loaded
            Globals.infoObservable.HandleNewGeoLocations += handleLiveGeoLocations;
            Globals.infoObservable.HandleAvailablePokeStop += InfoObservable_HandlePokeStop;
            Globals.infoObservable.HandlePokeStopInfoUpdate += InfoObservable_HandlePokeStopInfoUpdate;
            Globals.infoObservable.HandleNewShopGeoLocations += InfoObservable_HandleNewShopGeoLocations;
            Globals.infoObservable.HandleGetMonsterGeoLocations += InfoObservable_HandleGetMonsterGeoLocations;

            this.FormClosing += (object s, FormClosingEventArgs e) =>
            {                
                Globals.infoObservable.HandleNewGeoLocations -= handleLiveGeoLocations;
            };
        }

        private void InfoObservable_HandleGetMonsterGeoLocations(POGOProtos.Map.Pokemon.MapPokemon value, string message)
        {
            //抓到怪 
            int pokemonId = (int)value.PokemonId;
            var Sprites = AppDomain.CurrentDomain.BaseDirectory + "Sprites\\";
            string location = Sprites + pokemonId + ".png";
            if (!Directory.Exists(Sprites))
                Directory.CreateDirectory(Sprites);
            if (!File.Exists(location))
            {
                //插根圖釘
                GMarkerGoogle pokeStopMaker = new GMarkerGoogle(new PointLatLng(value.Latitude, value.Longitude), GMarkerGoogleType.blue_pushpin);
                _pokeStopsOverlay.Markers.Add(pokeStopMaker);
            }
            else
            {
                using (Stream bmpStream = System.IO.File.Open(location, System.IO.FileMode.Open))
                {
                    //插入怪物圖
                    Image image = Image.FromStream(bmpStream);
                    System.Drawing.Bitmap bitmap = Process(new Bitmap(image), 128, 128, 36, 36);
                    GMarkerGoogle pokeStopMaker = new GMarkerGoogle(new PointLatLng(value.Latitude, value.Longitude), bitmap);
                    pokeStopMaker.ToolTipText = message;
                    _pokeStopsOverlay.Markers.Add(pokeStopMaker);



                }

            }
        }

        private static Bitmap Process(Bitmap originImage, int oriwidth, int oriheight, int width, int height)
        {
            Bitmap resizedbitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(resizedbitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(originImage, new Rectangle(0, 0, width, height), new Rectangle(0, 0, oriwidth, oriheight), GraphicsUnit.Pixel);
            return resizedbitmap;
        }
        private void InfoObservable_HandleNewShopGeoLocations(GeoCoordinate value)
        {
            //新目標
            GMarkerGoogle pokeStopMaker = new GMarkerGoogle(new PointLatLng(value.Latitude, value.Longitude), GMarkerGoogleType.green_small);
            _pokeStopsOverlay.Markers.Add(pokeStopMaker);
        }

        private void InfoObservable_HandlePokeStopInfoUpdate(string pokeStopId, string info)
        {
            if (_pokeStopsMarks.ContainsKey(pokeStopId)) {
                //changeType
                var newMark = new GMarkerGoogle(_pokeStopsMarks[pokeStopId].Position, GMarkerGoogleType.blue_small);
                newMark.ToolTipText = info;
                newMark.ToolTip.Font = new System.Drawing.Font("Arial", 12, System.Drawing.GraphicsUnit.Pixel);
                
                try
                {
                    _pokeStopsOverlay.Markers[_pokeStopsOverlay.Markers.IndexOf(_pokeStopsMarks[pokeStopId])] = newMark;
                }
                catch(Exception e)
                {
                    Logger.ColoredConsoleWrite(ConsoleColor.Red, "[Debug] - Supressed error msg (Location.cs - Line 86 - Index is -1");
                }
                _pokeStopsMarks[pokeStopId] = newMark;
            }
        }

        private void InfoObservable_HandlePokeStop(POGOProtos.Map.Fort.FortData[] pokeStops)
        {
            _pokeStopsOverlay.Markers.Clear();
            _pokeStopsMarks.Clear();

            foreach (var pokeStop in pokeStops) {
                
                GMarkerGoogle pokeStopMaker = new GMarkerGoogle(new PointLatLng(pokeStop.Latitude, pokeStop.Longitude), GMarkerGoogleType.purple_small);
                if (pokeStop.LureInfo != null)
                {
                    pokeStopMaker = new GMarkerGoogle(new PointLatLng(pokeStop.Latitude, pokeStop.Longitude), GMarkerGoogleType.green_dot);
                }
                pokeStopMaker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                _pokeStopsMarks.Add(pokeStop.Id, pokeStopMaker);
                _pokeStopsOverlay.Markers.Add(pokeStopMaker);
            }
        }

        private GMapPolygon CreateCircle(PointLatLng point, double radius, int segments)
        {
            radius /= 100000;
            List<PointLatLng> gpollist = new List<PointLatLng>();
            double seg = Math.PI * 2 / segments;
            for (int i = 0; i < segments; i++)
            {
                double theta = seg * i;
                double a = point.Lat + Math.Cos(theta) * radius * 0.75;
                double b = point.Lng + Math.Sin(theta) * radius;
                gpollist.Add(new PointLatLng(a, b));
            }
            GMapPolygon circle = new GMapPolygon(gpollist, "BotZone");
            circle.Stroke = System.Drawing.Pens.Black;
            circle.Fill = System.Drawing.Brushes.Transparent;
            return circle;
        }

        private void handleLiveGeoLocations(GeoCoordinate coords)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                textBox1.Text = coords.Latitude.ToString();
                textBox2.Text = coords.Longitude.ToString();
                PointLatLng newPosition = new PointLatLng(coords.Latitude, coords.Longitude);
                _botMarker.Position = newPosition;
                _botRoute.Points.Add(newPosition);
                map.Position = newPosition;
            }));
        }

        private void map_Load(object sender, EventArgs e)
        {
            Globals.MapLoaded = true;
            showMap();
        }

        private void showMap()
        {
            try
            {
                map.DragButton = MouseButtons.Left;
                map.MapProvider = GMapProviders.GoogleMap;
                GMapProvider.Language = LanguageType.ChineseTraditional;
                map.Position = new GMap.NET.PointLatLng(Globals.latitute, Globals.longitude);
                map.MinZoom = 0;
                map.MaxZoom = 20;
                map.Zoom = 16;

                textBox1.Text = Globals.latitute.ToString();
                textBox2.Text = Globals.longitude.ToString();
                textBox3.Text = Globals.altitude.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void map_OnMapDrag()
        {
            Task.Run(() =>
            {
                var elevationRequest = new ElevationRequest()
                {
                    Locations = new[] { new Location(map.Position.Lat, map.Position.Lng) },
                };
                try
                {
                    ElevationResponse elevation = GoogleMaps.Elevation.Query(elevationRequest);
                    if (elevation.Status == Status.OK)
                    {
                        foreach (Result result in elevation.Results)
                        {
                            SetText(result.Elevation);
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            });

            textBox1.Text = map.Position.Lat.ToString(CultureInfo.InvariantCulture);
            textBox2.Text = map.Position.Lng.ToString(CultureInfo.InvariantCulture);
        }

        delegate void SetTextCallback(double cord);

        private void SetText(double cord)
        {
            if (this.textBox3.InvokeRequired)
            {
                SetTextCallback d = SetText;
                this.Invoke(d, cord);
            }
            else
            {
                this.textBox3.Text = cord.ToString(CultureInfo.InvariantCulture);
                this.alt = cord;
            }
        }

        private void LocationSelect_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (close)
            {
                var result = MessageBox.Show("You didn't set start location! Are you sure you want to exit this window?", "Location selector", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No || result == DialogResult.Abort)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Globals.latitute = map.Position.Lat;
            Globals.longitude = map.Position.Lng;
            Globals.altitude = alt;
            close = false;
            ActiveForm.Dispose();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0 && textBox1.Text != "-")
            {
                try
                {
                    double lat = double.Parse(textBox1.Text.Replace(',', '.'), GUI.cords, System.Globalization.NumberFormatInfo.InvariantInfo);
                    if (lat > 90.0 || lat < -90.0)
                        throw new System.ArgumentException("Value has to be between 180 and -180!");
                    map.Position = new GMap.NET.PointLatLng(lat, map.Position.Lng);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    textBox1.Text = "";
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length > 0 && textBox2.Text != "-")
            {
                try
                {
                    double lng = double.Parse(textBox2.Text.Replace(',', '.'), GUI.cords, System.Globalization.NumberFormatInfo.InvariantInfo);
                    if (lng > 180.0 || lng < -180.0)
                        throw new System.ArgumentException("Value has to be between 90 and -90!");
                    map.Position = new GMap.NET.PointLatLng(map.Position.Lat, lng);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    textBox2.Text = "";
                }
            }
        } 

        private void cbShowPokeStops_CheckedChanged(object sender, EventArgs e)
        {
            _pokeStopsOverlay.IsVisibile = cbShowPokeStops.Checked;
            map.Update();
        }
    }
}
