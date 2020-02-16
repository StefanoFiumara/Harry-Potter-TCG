using HarryPotter.Input.Controllers;
using HarryPotter.StateManagement;

namespace HarryPotter.Input
{
    public abstract class BaseControllerState : BaseState
    {
        public ClickToPlayCardController Owner { get; set; }
    }
}