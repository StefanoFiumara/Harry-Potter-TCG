using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HarryPotter.Views.UI.Tooltips
{
    public class UITooltipContent : MonoBehaviour, ITooltipContent, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea]
        public string DescriptionText;

        [TextArea] 
        public string ActionText;
        
        private TooltipController _controller;

        private void Awake()
        {
            _controller = FindObjectOfType<TooltipController>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _controller.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _controller.Hide();
        }

        public string GetDescriptionText() => DescriptionText;

        public string GetActionText(MonoBehaviour context = null) => ActionText;
    }
}