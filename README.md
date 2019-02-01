# ArcGIS Runtime SDK for Xamarin
This project was presented in the **2018 Esri Romania Users Conference** and in the **2018 ESRI Romania Webinar Session**.
For this demo, I used Visual Studio 2017 (C# and Xamarin technologies).
How can you use this project?
First of all, you need Visual Studio (2017 version is a good idea) and GitHub Extension for Visual Studio.
# How do you start to participate on this project?
## Starting with Visual Studio 2017
1. Open Visual Studio 2017
2. Displays the *Team Explorer Window* using **Ctrl +** \ or **Ctrl + M** or clicking on the *Team Explorer* option of *View* menu;
3. Click on Connect... (ellipsis option) on the GitHub option;
4. You must provide GitHub user and password and Sign In;
5. Using project url you can clone *ArcGIS Runtime SDK for Xamarin* on your computer;
## Starting with GitHub Desktop
For this step, you need to download and install GitHub Desktop.
1. Open GitHub Desktuop and log-in using GitHub account;
2. On the Current Repository (left top) click on Add button;
3. Click URL tab. Using project url you can clone *ArcGIS Runtime SDK for Xamarin* on your computer
# Visual Studio solution
1. Open Visual Studio solution;
## What you can find?
2. Into the Visual Studio solution, you can find 4 projects:

    *ArcGISX1.Android* for Android OS;
	
    *ArcGISX1.iOS*     for iOS OS;
	
    *ArcGISX1.UWP*     for Windows OS;
	
    *ArcGISX1.Shared*  developing project;

3. select *ArcGISX1.Shared* and open *MapViewModel.cs* class. The *ArcGISX1.Shared* project is place where you can add more functionalities;
4. You must change the url from line **49**. A good ideea is to start with project url. Is a public url. So, paste your url into the code;
For just a secunde you can look at the line **49**. The url from line **49** is a featureserver service. What this means??
No matter how many layers you have into feature server service. The application will use all the layers in the service. 
You just discovered the first customization. 
## What can you give up?
*ArcGISX1.Shared\util\intrare.cs* is a coordinate simulator. It's just for the demo, nothing else.
# Where to add more functionalities?
You can add more editing functionalities.
1. For add points on the data, you can add more code into *Adaugare* async method from *MapViewModel.cs* (section 7);

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
        
	Just a look on this sample. 
	Into **359** line, you can add attributes values for point geometry:
			
			feature.Attributes["Field"] = "Value";
	or:
	
			feature.SetAttributeValue("Field", "Value");
	
2. For change point position, you can add mode code into *_modificare_geometrieAsync* async metod form *MapViewModel.cs*; 

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
			
	Into **415** line, you can change existing attributes values for point geometry, as above (point 1):
	
			feature.Attributes["Field"] = "Value";
	or:
	
			feature.SetAttributeValue("Field", "Value");

You can change selection method.
	
3. For another selection method, you can change *_selectare_elementeAsync* task from *MapViewModel.cs*;

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

	For example, you can change the selection geometry or another selection particularities;

You can change Geofence functionalities;

4. All the Geofence functionalities are found into *ArcGISX1.Shared\util\simulator.cs*;
 
Here is a little more to talk about this:
 
For spatial reference and buffer dimension:

            MapPoint mp = new MapPoint(coordinate.Item1, coordinate.Item2, new SpatialReference(3857));
            MapPointBuilder mappoint_builder = new MapPointBuilder(mp);
            Geometry buffer_geometry = GeometryEngine.Buffer(mappoint_builder.ToGeometry(), 30); 
 
You can change wkid spatial reference  into first line above. Also, you can change buffer dimension into third line above.

On the other hand, you can make customisation of the notifications:

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
