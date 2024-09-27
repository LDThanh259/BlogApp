namespace AppMVC.Models
{
    public class SummerNoteModel
    {
        public string EditorId { get; set; }

        public bool LoadLib { get; set; }

        public int height { get; set; } = 120;

        public string toolbar { get; set; } = @"
            [
          ['style', ['style']],
          ['font', ['bold', 'underline', 'clear']],
          ['color', ['color']],
          ['para', ['ul', 'ol', 'paragraph']],
          ['table', ['table']],
          ['insert', ['link', 'picture', 'video','elfinder']],
          ['view', ['fullscreen', 'codeview', 'help']]
        ]
            ";
        public SummerNoteModel(string editorId, bool loadLib = true)
        {
            EditorId = editorId;
            LoadLib = loadLib;
        }
    }
}
