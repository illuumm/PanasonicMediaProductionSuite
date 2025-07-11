using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanasonicMediaProductionSuite
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AutoStartArea
    {
        public int AutoStartAreaEnable { get; set; }
        public List<List<int>> polygon { get; set; }
    }

    public class CameraInfo
    {
        public string IP_address { get; set; }
        public List<int> PanTiltLimitUDLR { get; set; }
        public string guid { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int powermode { get; set; }
    }

    public class FramingState
    {
        public bool AutoFaceSearch { get; set; }
        public bool AutoZoom { get; set; }
        public int FramingEnable { get; set; }
        public int FramingStartStop { get; set; }
        public int FramingStatus { get; set; }
        public TargetFace TargetFace { get; set; }
        public TrackingControl TrackingControl { get; set; }
        public AutoStartArea auto_start_area { get; set; }
        public CameraInfo camera_info { get; set; }
        public MaskArea mask_area { get; set; }
        public PtzStatus ptz_status { get; set; }
        public int selected_id { get; set; }
        public TargetFrame target_frame { get; set; }
        public List<int> target_id { get; set; }
    }

    public class MaskArea
    {
        public List<List<List<int>>> polygon_array { get; set; }
    }

    public class PtzStatus
    {
        public bool ptz_move { get; set; }
    }

    public class FramingStateRoot
    {
        public string Command { get; set; }
        public List<FramingState> FramingState { get; set; }
        public string Parameter { get; set; }
        public string Response { get; set; }
    }

    public class TargetFace
    {
        public List<object> list_id { get; set; }
        public List<object> name { get; set; }
    }

    public class TargetFrame
    {
        public double pos_x { get; set; }
        public double pos_y { get; set; }
        public double zoom { get; set; }
    }

    public class TrackingControl
    {
        public int AutoZoomSpeed { get; set; }
        public int PanTiltSpeed { get; set; }
        public int Sensitivity { get; set; }
    }


}
