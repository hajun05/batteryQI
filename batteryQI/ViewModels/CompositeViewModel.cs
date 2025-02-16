using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace batteryQI.ViewModels
{
    // InspectViewModel과 ManagerViewModel을 동시에 활용하여 화면을 구성하는 View를 위해 둘을 합친 클래스
    class CompositeViewModel
    {
        public InspectViewModel InspectViewModel { get; set; }
        public ManagerViewModel ManagerViewModel { get; set; }

        public CompositeViewModel()
        {
            InspectViewModel = new InspectViewModel();
            ManagerViewModel = new ManagerViewModel();
        }
    }
}
