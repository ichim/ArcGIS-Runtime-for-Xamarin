using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using Xamarin.Forms;
using System.Windows.Input;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.ArcGISServices;
using System.IO;

namespace ArcGISX1.Shared
{
    /// <summary>
    /// Provides map data to an application
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {


        
        public MapViewModel()
        {
            _map = new Map(BasemapType.Streets,   44.431524, 26.102513, 12);
        }

        private MapView CurrentMapView;
        ToLocalGeodatabase local;

        public MapView SetCurrentMapView
        {
            get {
                
                return CurrentMapView; }
            set {
               
                CurrentMapView = value;
                local = new ToLocalGeodatabase("https://services6.arcgis.com/Uwg97gPMK3qqaMen/arcgis/rest/services/incidente_securitate/FeatureServer", CurrentMapView, _progres, _label_info, _selectie_adaugare_switch, local_button, sync_button);
            }
        }


        private ActivityIndicator _progres;

        public ActivityIndicator bara_progres
        {
            get { return _progres; }
            set { _progres = value; }
        }



        private Map _map;

        /// <summary>
        /// Gets or sets the map
        /// </summary>
        public Map Map
        {
            get {
                

                return _map;
            }
            set { _map = value; OnPropertyChanged(); }
        }


        private Label _label_info;

        public Label Label_info
        {
            get { return _label_info; }
            set { _label_info = value;  }
        }


        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private Color _color_plus = Color.FromRgb(16,152,167);

        public Color _button_color_plus
        {
            get { return _color_plus; }
            set { _color_plus  = value; }
        }


        private Color _color_minus = Color.FromRgb(16, 152, 167);

        public Color _button_color_minus
        {
            get { return _color_minus; }
            set { _color_minus = value; }
        }

        private Color _color_rot = Color.FromRgb(16, 152, 167);

        public Color _button_color_rot
        {
            get { return _color_rot; }
            set { _color_rot = value; }
        }

        private Color _color_sync = Color.FromRgb(187, 47, 6);

        public Color _button_color_sync
        {
            get { return _color_sync; }
            set { _color_sync = value; }
        }

        private Color _color_loc = Color.FromRgb(118, 176, 66);

        public Color _button_color_loc
        {
            get { return _color_loc; }
            set { _color_loc = value; }
        }

        private Switch _selectie_adaugare_switch;

        public Switch Switch_Selectie_Adaugare
        {
            get { return _selectie_adaugare_switch; }
            set { _selectie_adaugare_switch = value; }
        }

        private Button local_button;

        public Button Local_Button
        {
            get { return local_button; }
            set { local_button = value; }
        }


        private Button sync_button;

        public Button Sync_Button
        {
            get { return sync_button; }
            set { sync_button = value; }
        }



        public void TappedOnMapView(object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            local.Move_or_Add_on_Tapped(sender, e);
        }


        public void zoom(string type, MapView mapView )
        {
            var scara = mapView.MapScale;
            var rot = mapView.MapRotation;
            switch(type)
            {
                case "minus":
                    scara = scara * 2;
                    mapView.SetViewpointScaleAsync(scara);
                    break;
                case "plus":
                    scara = scara / 2;
                    mapView.SetViewpointScaleAsync(scara);
                    break;
                case "rot":
                    mapView.SetViewpointRotationAsync(rot+=5);
                    break;

            }
        }

        public async Task sicronizareAsync()
        {
            await local.Sincronizare_cu_enterprise_geodatabase();
        }

        public async Task localAsync()
        {
           
            await local.StartGeodatabaseGeneration();
        }

    }




    public class ToLocalGeodatabase
    {
        
        private Uri _featureServiceUri;
        private Map _map;
        private GeodatabaseSyncTask _gdb_local;
        private Geodatabase _gdb;
        private string _gdb_path;
        private EditState _readyForEdits = EditState.NotReady;
        private ActivityIndicator progres = null;
        private MapView CurrentMapView;
        private Label informatii = null;
        private Switch Switch_selectie = null;
        private Button Local_button = null;
        private Button Sync_button = null;

        public   ToLocalGeodatabase(
            string url, 
            MapView _currentMapView, 
            ActivityIndicator _progres, 
            Label _informatii, 
            Switch switch_selectie,
            Button local,
            Button sync)
        {
             _featureServiceUri = new Uri(url);
            CurrentMapView = _currentMapView;
            progres = _progres;
            informatii = _informatii;
            Switch_selectie = switch_selectie;
            Local_button = local;
            Sync_button = sync;
            _gdb_path = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), Path.GetTempFileName() + ".geodatabase");

        }

        private enum EditState
        {
            NotReady, // Geodatabase-ul local inca nu a fost generat
            Editing, // Elementele pot fi editate/mutate
            Ready // Acum baza de date poate fi sincronizata cu serverul
        }

        public async Task StartGeodatabaseGeneration()
        {
            GenerateGeodatabaseJob generateGdbJob;
            progres.IsRunning = true;
            informatii.Text = "Online to Local ...";

            /*3 local / Copie locala gdb (3)*/
            // taskul cu care se creaza geodatabase (GeodatabaseSyncTask).
            _gdb_local = await GeodatabaseSyncTask.CreateAsync(_featureServiceUri);
            // Exportul datelor se va face doar pentru graficul din view. Se extrage acest grafic de pe MapView
            Graphic redPreviewBox = CurrentMapView.GraphicsOverlays.First().Graphics.First();
            // Se extrage extentul graficului.
            Envelope extent = redPreviewBox.Geometry as Envelope;
            // Se genereaza parametrii pentru generarea geodatabase.
            GenerateGeodatabaseParameters parametri = await _gdb_local.CreateDefaultGenerateGeodatabaseParametersAsync(extent);
            // crearea unui job pentru generarea geodatabase.
            generateGdbJob = _gdb_local.GenerateGeodatabase(parametri, _gdb_path);
            // lansati job-ul.
            generateGdbJob.Start();
            /*3*/

            // obtineti rezultatele job-ului.
            _gdb = await generateGdbJob.GetResultAsync();

            // Date/mesaje.
            Continut_offline_status(generateGdbJob);
        }

        private void Continut_offline_status(GenerateGeodatabaseJob job)
        {
            // Daca job-ul este rulat cu succes, datele vor fi adaugate la harta inlocuind layerele adaugate anterior.
            if (job.Status == JobStatus.Succeeded)
            {

                /*4 local / Adaugare layere in harta (4)*/
                // Se inlatura layerele existente.
                CurrentMapView.Map.OperationalLayers.Clear();
                // Se parcuge continutul geodatabase si sunt adaugate layere la harta.
                foreach (GeodatabaseFeatureTable table in _gdb.GeodatabaseFeatureTables)
                {
                    // Se creaza un feature layer din tabel.
                    FeatureLayer layer = new FeatureLayer(table);
                    // Se adauga layer-ul la harta.
                    CurrentMapView.Map.OperationalLayers.Add(layer);
                }
                /*4*/

                // Se activeaza editarea feature-lor.
                _readyForEdits = EditState.Ready;
            }
            else
            {
                // In cazul in care job-ul nu a fost rulat cu succes.
                string message = "Generate geodatabase job failed";
                // In cazul in care exista si un mesaj de eroare.
                if (job.Error != null)
                {
                    message += ": " + job.Error.Message;
                }
                else
                {
                    // In cazul in care nu exista mesaje de eroare.
                    foreach (JobMessage m in job.Messages)
                    {
                        // Se extrage text din JobMessage si este adaugat la mesaj.
                        message += "\n" + m.Message;
                    }
                }
            }
            progres.IsRunning = false;
            informatii.Text = "Local / Click pe S pentru sincronizare";
            butoane_sincronizare(false);
        }


        public  void Move_or_Add_on_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Renunta daca nu este disponibila editarea.
            if (_readyForEdits == EditState.NotReady) { return; }


            if (!Switch_selectie.IsToggled)
            {
               selectare_modifcare_elemente(sender, e);
            }
            else
            {
                Adaugare(sender, e);
            }
        }


        public async void Adaugare(object sender, GeoViewInputEventArgs e)
        {
             
            /*7 Adaugare de geometrie*/
                foreach (FeatureLayer layer in CurrentMapView.Map.OperationalLayers)
                {
                    layer.ClearSelection();
                    FeatureTable tabel = layer.FeatureTable;
                    Feature feature = tabel.CreateFeature();
                    feature.Geometry = e.Location;
                    try {
                        await tabel.AddFeatureAsync(feature);

                    } catch(Esri.ArcGISRuntime.ArcGISRuntimeException ex) {
                   
                        informatii.Text = "Eroare: " + ex.ErrorCode.ToString();
                    }
                   
                }
                /*7*/
        }

        public async void selectare_modifcare_elemente (object sender, GeoViewInputEventArgs e)
        {
            // In cazul in care procesul de editare este disponibil, se modifica geometriile elementelor selectate.
            if (_readyForEdits == EditState.Editing)
            {
                await _modificare_geometrieAsync(e);
            }
            //In cazul in care editarea nu este disponivila, se efectueaza selectari in harta.
            else
            {
                //selectare elemente in harta
                await _selectare_elementeAsync(e, 15);
            }
        }

        private async Task _modificare_geometrieAsync(GeoViewInputEventArgs e)
        {
            // Se populeaza o lista cu elementele selectate.
            List<Feature> selectedFeatures = new List<Feature>();

            /*5 local / Creare lista de elemente selectate (5)*/
            // Se incarca lista cu elementele selectate si se sterge selectia.
            foreach (FeatureLayer layer in CurrentMapView.Map.OperationalLayers)
            {
                // Elementele selectate din layer-ul curent.
                FeatureQueryResult layerFeatures = await layer.GetSelectedFeaturesAsync();
                // FeatureQueryResult este implementat de IEnumerable, deci aceasta poate fi parcursa ca o colectie de features.
                selectedFeatures.AddRange(layerFeatures);
                // Se sterge selectia.
                layer.ClearSelection();
            }
            /*5*/


            /*6 local / Actualizare geometrie (6)*/
            // se face actualizarea geometriei feature-urilor selectate.
            foreach (Feature feature in selectedFeatures)
            {
                // Se obtine referinta tabelului din care face parte feature-ul curent.
                GeodatabaseFeatureTable table = (GeodatabaseFeatureTable)feature.FeatureTable;
                // Verificati daca geometria este de tip punct.
                if (table.GeometryType == GeometryType.Point)
                {
                    feature.Geometry = e.Location;  //se atribuie noua geometrie              
                    try
                    {
                        // Se actualziaeaza feature-ul din tabel.
                        await table.UpdateFeatureAsync(feature);   
                    }
                    catch (Esri.ArcGISRuntime.ArcGISException)    
                    { }
                }
            }
            /*6*/

            // Se actuazlizeaza starea ditorului.
            _readyForEdits = EditState.Ready;
        }

        private async Task _selectare_elementeAsync(GeoViewInputEventArgs e, double pixels_tolerance)
        {
            // Toleranta utilizata pentru identify/selectare feature.
            double tolerance = pixels_tolerance * CurrentMapView.UnitsPerPixel;
            // Se defineste extent-ul pentru selectie.
            Envelope selectionEnvelope = new Envelope(e.Location.X - tolerance, e.Location.Y - tolerance, e.Location.X + tolerance, e.Location.Y + tolerance);

            // Se creaza (new) un obiect pentru intergoare.
            QueryParameters query = new QueryParameters()
            {
                Geometry = selectionEnvelope
            };

            // Se selecteaza toate feature-urile din tabele aplicate.
            foreach (FeatureLayer layer in CurrentMapView.Map.OperationalLayers)
            {
                await layer.SelectFeaturesAsync(query, SelectionMode.New);
            }
            // Se schimba starea editorului (conform cu definitia).
            _readyForEdits = EditState.Editing;
        }



        private void butoane_sincronizare(bool activat)
        {
            Local_button.IsEnabled = activat;
            Sync_button.IsEnabled = !Local_button.IsEnabled;
        }

        public async Task Sincronizare_cu_enterprise_geodatabase()
        {
            progres.IsRunning = true;
            // Renunta daca Editorul nu este pregatit.
            if (_readyForEdits != EditState.Ready) { return; }

            /*8 sincronizare / Sincronizare local - enterprise*/
            // Se creaza <parametru pentru sincronizarea geodatabase>.
            SyncGeodatabaseParameters parameters = new SyncGeodatabaseParameters()
            {
                GeodatabaseSyncDirection = SyncDirection.Bidirectional,
                RollbackOnFailure = false
            };
            // Se obtine Layer ID pentru fiecare tabel din geodatabase local si se adauga in <sync job>.
            foreach (GeodatabaseFeatureTable table in _gdb.GeodatabaseFeatureTables)
            {
                // Se initializeaza id cu id-ul layer-ului.
                long id = table.ServiceLayerId;
                // Se creaza un obiect <SyncLayerOption>.
                SyncLayerOption option = new SyncLayerOption(id);
                // se adauga la <parametru pentru sincronizarea geodatabase>.
                parameters.LayerOptions.Add(option);
            }

            // Se creaza job.
            SyncGeodatabaseJob job = _gdb_local.SyncGeodatabase(parameters, _gdb);
            // se lanseaza in executie job-ul.
            job.Start();
            // Se asteapta pana la obtinerea rezultatului.
            await job.GetResultAsync();
            /*8*/


            // Mesaje referitoare la stadiu sincronizarii.
            Status_Sincronizare(job);


            progres.IsRunning = false;
        }

        private void Status_Sincronizare(SyncGeodatabaseJob job)
        {
            // Se verifica daca job-ul a esuat.
            if (job.Status == JobStatus.Failed)
            {
                // Compuneti un mesaj pentru utilizator.
                string message = "Sync geodatabase job failed";

                // In cazul in care exista un mesaj, acesta va fi afisat.
                if (job.Error != null)
                {
                    message += ": " + job.Error.Message;

                }
                else
                {
                    // In cazul in care nu exista mesaje de eroare, afisam mesajul job-ului.
                    foreach (JobMessage m in job.Messages)
                    {
                        // Obtinem mesajul de pe <JobMessage> si il adaugam mesajului.
                        message += "\n" + m.Message;
                    }

                }

                // Afisam mesajul.
                informatii.Text = "Eroare la sincronizare...";
            }
            else
            {
                informatii.Text = "Sincronizare reusita";
            }
            // butoane_sincronizare(true);
        }
    }
}
