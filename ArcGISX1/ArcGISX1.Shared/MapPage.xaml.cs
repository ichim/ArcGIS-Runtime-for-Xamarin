
using ArcGISX1.Shared;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using System.Threading;
using System.Threading.Tasks;


using ArcGISX1.util;

namespace ArcGISX1
{
	public partial class MapPage : ContentPage
    {
        MapViewModel mvm;
        bool grid_ajutator = false;
   
        public MapPage()
        {
            
            mvm = new MapViewModel();
          
            BindingContext = mvm;
            


            InitializeComponent();
            mvm.bara_progres = progres;
            mvm.Label_info = Informatii;
            mvm.Switch_Selectie_Adaugare = onoff_Add;
            mvm.Local_Button = local;
            mvm.Sync_Button = sincronizare;
            mvm.SetCurrentMapView = CurrentMapView;
          


            SimpleLineSymbol lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 2);
            /*Se creaza limita zonei de export*/
            GraphicsOverlay extentOverlay = new GraphicsOverlay
            {
                Renderer = new SimpleRenderer(lineSymbol)
            };

            // Se adauga limita zonei de export la MapView.
            CurrentMapView.GraphicsOverlays.Add(extentOverlay);



            CurrentMapView.GraphicsOverlays.Add(overlay_buffer);
            overlay_buffer.IsVisible = true;

            CurrentMapView.GraphicsOverlays.Add(overlay_tracking);
            overlay_tracking.IsVisible = true;


            CurrentMapView.GraphicsOverlays.Add(overlay_notificari);
            overlay_notificari.IsVisible = true;



            CurrentMapView.ViewpointChanged += CurrentMapView_ViewpointChanged;

            this.Informatii.Text = "Activati gridul si copiati datele local";
        }

        private void CurrentMapView_ViewpointChanged(object sender, System.EventArgs e)
        {
            if(grid_ajutator)
            UpdateGridAjutator();
        }


        private void UpdateGridAjutator()
        {
            // Renunta daca MapView-ul este null.
            if (CurrentMapView == null) { return; }

            // view-ul curent.
            Viewpoint pointView = CurrentMapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry);
            
            //Renunta daca view-ul este nul.
            if (pointView == null) { return; }

            // Extentul view-ului.
            Envelope extent = pointView.TargetGeometry as Envelope;

            // Renunta daca view-ul este null.
            if (extent == null) { return; }

            // Creaza zona ajutatoare care va fi cica 80% din suprfata view-ului curent.
            EnvelopeBuilder envelopeBldr = new EnvelopeBuilder(extent);
            envelopeBldr.Expand(0.80);


            // Recuperez primul/implicit grafic overlay din MapView (este creat la initializare).
            GraphicsOverlay extentOverlay = CurrentMapView.GraphicsOverlays.FirstOrDefault();

            //Renunta daca este null.
            if (extentOverlay == null) { return; }

            // Recuperez graficul (dreptunghi) in cazul in care acesta este deja desenat (exista).
            Graphic extentGraphic = extentOverlay.Graphics.FirstOrDefault();

            // In cazul in care acesta (dreptunghi) nu exista se va crea un grid ajutator (graficul desenat).
            if (extentGraphic == null)
            {
                extentGraphic = new Graphic(envelopeBldr.ToGeometry());

                extentOverlay.Graphics.Add(extentGraphic);
            }
            else
            {
                // In cazul in care exista, este actualizata doar geometria (graficul desenat - dreptunghi).
                extentGraphic.Geometry = envelopeBldr.ToGeometry();
            }
        }
        private void Button_Clicked_plus(object sender, System.EventArgs e)
        {
            mvm.zoom("plus", CurrentMapView);
           


        }


        private void Button_Clicked_minus(object sender, System.EventArgs e)
        {
            mvm.zoom("minus", CurrentMapView);
        }


        private void Button_Clicked_rot(object sender, System.EventArgs e)
        {
            mvm.zoom("rot", CurrentMapView);
        }
        // Map initialization logic is contained in MapViewModel.cs


        private async void Button_Clicked_syncAsync(object sender, System.EventArgs e)
        {
            await mvm.sicronizareAsync();
        }

        private async void Button_Clicked_localAsync(object sender, System.EventArgs e)
        {
            await mvm.localAsync();
        }

        private void onoff__Toggled(object sender, ToggledEventArgs e)
        {
            grid_ajutator = e.Value;
            UpdateGridAjutator();
            local.IsEnabled = e.Value;
            sincronizare.IsEnabled = !local.IsEnabled;
            if(!e.Value)
            {
                GraphicsOverlay extentOverlay = CurrentMapView.GraphicsOverlays.FirstOrDefault();
                extentOverlay.Graphics.Clear();

            }
            this.Informatii.Text = "Click pe L pentru efectua o copie locala";
        }

        private void onoff__Toggled_add(object sender, ToggledEventArgs e)
        {

        }

        private void CurrentMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            mvm.TappedOnMapView( sender,e);
        }

        private readonly GraphicsOverlay overlay_tracking = new GraphicsOverlay();
        private readonly GraphicsOverlay overlay_buffer = new GraphicsOverlay();
        private readonly GraphicsOverlay overlay_notificari = new GraphicsOverlay();

        private void tracking_Toggled(object sender, ToggledEventArgs e)
        {

            if (e.Value)
            {       //se va lansa trackingul. Se vor porni evenimentele de tracking
                    Simulator simulator = new Simulator(overlay_tracking, overlay_buffer, CurrentMapView, overlay_notificari);
                    // simulator.OnGeofenceEvent += Simulator_OnGeofenceEvent;
            }
            else
            {//se vor opri evenimentele de tracking

            }
            System.Diagnostics.Debug.WriteLine(e.Value, "tracking_Toggled"); 
        }

        private void Simulator_OnGeofenceEvent(MapPoint pct)
        {
            Informatii.Text = pct.X.ToString();
        }

        //ViewModel
        private void Button_Clicked(object sender, EventArgs e)
        {//Bookmark 1 - detaliu pentru copie locala  
            MapPoint bookmark_point = new MapPoint(26.096136, 44.441888, SpatialReferences.Wgs84);

            Viewpoint view_point = new Viewpoint(bookmark_point, 5000);
            CurrentMapView.SetViewpointAsync(view_point, new TimeSpan(100));

        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {//Bookmark 2 - detaliu pentru mutare geometrie
            MapPoint bookmark_point = new MapPoint(26.097307, 44.445119, SpatialReferences.Wgs84);

            Viewpoint view_point = new Viewpoint(bookmark_point, 10000);
            CurrentMapView.SetViewpointAsync(view_point, new TimeSpan(100));
        }
    }
}
