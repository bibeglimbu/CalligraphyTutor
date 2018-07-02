using Microsoft.Win32;
using System.IO;
using System.Windows.Ink;

namespace CalligraphyTutor.Model
{
        public class FileManager
        {

            /// <summary>
            /// Method to save the strokes from the student canvas as expert recordings to a file
            /// </summary>
            /// <param name="strokeCollection"></param>
            public void SaveStroke(StrokeCollection strokeCollection)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "isf files (*.isf)|*.isf";

                if (saveFileDialog1.ShowDialog() == true)
                {
                    FileStream fs = new FileStream(saveFileDialog1.FileName,
                                                   FileMode.Create);
                    //Debug.WriteLine(inkCanvas.Strokes.Count);
                    strokeCollection.Save(fs);
                    fs.Close();
                }
            }

            /// <summary>
            /// Methods to load the strokes to a file
            /// </summary>
            /// <returns></returns>
            public StrokeCollection LoadStroke()
            {
                StrokeCollection _loadedStrokes = new StrokeCollection();
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "isf files (*.isf)|*.isf";

                if (openFileDialog1.ShowDialog() == true)
                {
                    using (FileStream fs = new FileStream(openFileDialog1.FileName,
                                                       FileMode.Open))
                    {
                        //inkCanvas.Strokes = new StrokeCollection(fs);
                        _loadedStrokes = new StrokeCollection(fs);
                    }
                    //fs.Close();

                }
                return _loadedStrokes;
            }

            /// <summary>
            /// Method to clear the strokes from the Inkcanvas
            /// </summary>
            /// <param name="strokeCollection"></param>
            public void ClearStroke(StrokeCollection strokeCollection)
            {
                strokeCollection.Clear();
            }
    }
}
