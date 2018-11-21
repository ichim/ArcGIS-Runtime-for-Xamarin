using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ArcGISX1.util
{
    public class Simulator
    {
        public delegate void OnGeofenceEventHandler(MapPoint pct);
        public event OnGeofenceEventHandler OnGeofenceEvent;
        public Simulator(GraphicsOverlay  overlay_tracking, GraphicsOverlay overlay_buffer, MapView CurrentMapView, GraphicsOverlay overlay_notificari)
        {
            Intrare date_intrare = new Intrare();


            Task.Run(() => { /* Simulatorul se va rula pe un task (fir de executie) astfel incat
                                    sa nu blocheze aplicatia (celalalte functionalitati)
                                    */
                foreach (var coordinate in date_intrare.coordinates)
                {

                    #region simbol alerta
                    SimpleMarkerSymbol cross_symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.Red, 16);
                    SimpleFillSymbol buffer_symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Color.FromRgba(255, 255, 0, 0.5), null);
                    TextSymbol notificare_symbol = new TextSymbol("Alerta", Color.Black, 12, HorizontalAlignment.Right, VerticalAlignment.Top);
                    #endregion

                    #region geometrie alerta
                    MapPoint mp = new MapPoint(coordinate.Item1, coordinate.Item2, new SpatialReference(3857));
                    MapPointBuilder mappoint_builder = new MapPointBuilder(mp);
                    Geometry buffer_geometry = GeometryEngine.Buffer(mappoint_builder.ToGeometry(), 30);
                    #endregion

                    OnGeofenceEvent?.Invoke(mp);

                  




                    var operational_layers = CurrentMapView.Map.OperationalLayers;
                    foreach (var layer in operational_layers)
                    {
                        #region Interogare pe layer-ul curent (incidente) pentru a verifica daca in buffer se afla vreun incident
                        FeatureLayer feature_layer = layer as FeatureLayer;
                        FeatureTable feature_table = feature_layer.FeatureTable;
                        //parametru pentru interogare
                        QueryParameters query_parameters = new QueryParameters()
                        {
                            Geometry = buffer_geometry,
                            SpatialRelationship = SpatialRelationship.Contains
                        };
                        //rezultatul interogarii
                       var results = feature_table.QueryFeaturesAsync(query_parameters);
                        //numarul de incidente aflate in buffer
                       int numar = results.Result.Count();
                        #endregion

                        if(numar!=0)
                        {

                            #region afisate alerta in harta in harta atuncti cand avem cel putin un incident in buffer
                            Graphic graphic_notify = overlay_notificari.Graphics.FirstOrDefault();
                            if(graphic_notify == null)
                            { //in cazul in care in  harta nu avem grafic notificare se va crea unul
                                 graphic_notify = new Graphic(mappoint_builder.ToGeometry(), notificare_symbol);
                                overlay_notificari.Graphics.Add(graphic_notify);
                                if(results.Result.First().Attributes["TIPFAPTAPE"] is null ==false)
                                    notificare_symbol.Text = "Alerta: \n" + results.Result.First().Attributes["TIPFAPTAPE"].ToString();
                                else
                                    notificare_symbol.Text = "Alerta!" ;
                            }
                            else
                            {//daca in harta avem o notificare, acesteia i se va modifica geometria si textul notificarii
                                graphic_notify.Geometry = mappoint_builder.ToGeometry();
                                if (results.Result.First().Attributes["TIPFAPTAPE"] is null == false)
                                    notificare_symbol.Text = "Alerta: \n" + results.Result.First().Attributes["TIPFAPTAPE"].ToString();
                                else
                                    notificare_symbol.Text = "Alerta!";
                                graphic_notify.Symbol = notificare_symbol;
                            }
                            #endregion
                        }



                    }

                    Graphic graphic_position = overlay_tracking.Graphics.FirstOrDefault();
                    Graphic graphic_buffer = overlay_buffer.Graphics.FirstOrDefault();

                    //afisarea pozitiei curente
                    if (graphic_position == null)
                    {//in cazul in care nu avem un grafic pentru pozitia curenta se va crea unul
                        graphic_position = new Graphic(mappoint_builder.ToGeometry(), cross_symbol);
                        overlay_tracking.Graphics.Add(graphic_position);
                    }
                    else
                    {//in cazul in care avem in grafic, se va actualiza doar geometria
                        graphic_position.Geometry = mappoint_builder.ToGeometry();
                    }

                    //afisarea buffer-ului in harta
                    if (graphic_buffer == null)
                    {//in cazul in care nu avem un grafic in harta, se va crea un astfel de grafic
                        graphic_buffer = new Graphic(buffer_geometry, buffer_symbol);
                        overlay_buffer.Graphics.Add(graphic_buffer);
                    }
                    else
                    {//in cazul in care exsita un grafic, acestiua i se va actualiza doar geometria
                        graphic_buffer.Geometry = buffer_geometry;
                    }


                    SetExtent(overlay_tracking, CurrentMapView);
                    Thread.Sleep(1500);
                }


            });
        }

        private void SetExtent(GraphicsOverlay overlay_tracking, MapView CurrentMapView)
        {
            // Se recupereaza toate graficile existente pe un <graphics overlay>
            GraphicCollection myGraphicCollection = overlay_tracking.Graphics;

            // Se creaza un nou envelope_builder utilizand acelasi spatial reference ca al graficelelor recuperate
            EnvelopeBuilder myEnvelopeBuilder = new EnvelopeBuilder(new SpatialReference(3857));

            // Se parcurge colectia de grafice
            foreach (Graphic oneGraphic in myGraphicCollection)
            {
                // Se face Union pe extent-ul fiecarul grafic in envelope_builder
                myEnvelopeBuilder.UnionOf(oneGraphic.Geometry.Extent);
            }

            // Se face expand la envelope_builder cu 30% 
            myEnvelopeBuilder.Expand(1.3);

            // Se corecteaza zona vizibila a hartii astfel incat sa fie afisate toate graficile din overlay_tracking
            // la acest extend se face expand cu 30% astfel incat sa fie cuprinse toate graficile in interiorul view-ului curent
            CurrentMapView.SetViewpointAsync(new Viewpoint(myEnvelopeBuilder.Extent));
        }
    }
}
