
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WaveParticleSim
{
    [RequireComponent(typeof(Slider))]
    public class UdonSlider : MonoBehaviour
    {
        private Slider mySlider;
        [SerializeField]
        private TextMeshProUGUI sliderLabel;
        [SerializeField]
        private TextMeshProUGUI sliderTitle;
        [SerializeField, Tooltip("Apply damping by setting above zero")]
        private float smoothRate = 2f;
        [SerializeField]
        private bool hideLabel = false;
        [SerializeField]
        private bool unitsInteger = false;
        [SerializeField]
        private bool displayInteger = false;
        [SerializeField]
        private string sliderUnit;
        [SerializeField]
        MonoBehaviour SliderClient;
        [SerializeField]
        string clientVariableName = "SliderValueVar";
        [SerializeField]
        string clientPtrEvent = "slidePtr";
        private float reportedValue;
        [SerializeField]
        private float syncedValue;
        private float targetValue;
        [SerializeField]
        private float maxValue = 1;
        [SerializeField]
        private float minValue = 0;

        [SerializeField]
        private bool interactable = true;
        private bool started = false;
        const float thresholdScale = 0.002f;
        float smoothThreshold = 0.003f;

        private bool pointerIsDown = false;
        public bool PointerIsDown
        {
            get => pointerIsDown;
        }

        public bool Interactable
        {
            get
            {
                return interactable;
            }
            set
            {
                interactable = value;
                if (mySlider != null)
                    mySlider.interactable = value;
            }
        }

        public void SetValue(float value)
        {
            if (!started)
            {
                syncedValue = value;
                reportedValue = value;
                return;
            }
            mySlider.SetValueWithoutNotify(value);
            SetSmoothingTarget(value);
            if (reportedValue != syncedValue)
                ReportedValue = syncedValue;
        }

        private void updateThreshold()
        {
            smoothThreshold = thresholdScale * Mathf.Max(0.01f, Mathf.Abs(maxValue - minValue));
        }

        public void SetLimits(float min, float max)
        {
            if (mySlider == null)
                mySlider = GetComponent<Slider>();
            minValue = min;
            maxValue = max;
            updateThreshold();
            if (!started)
                return;
            mySlider.minValue = minValue;
            mySlider.maxValue = maxValue;
        }
        public string TitleText
        {
            get
            {
                if (sliderTitle == null)
                    return "";
                return sliderTitle.text;
            }
            set
            {
                if (sliderTitle == null)
                    return;
                sliderTitle.text = value;
            }
        }
        public string SliderUnit
        {
            get => sliderUnit;
            set
            {
                sliderUnit = value;
                Updatelabel();
            }
        }
        private void Updatelabel()
        {
            if (sliderLabel == null || hideLabel)
                return;
            float displayValue = syncedValue;
            if (displayInteger)
                displayValue = Mathf.RoundToInt(displayValue);
            if (unitsInteger || displayInteger)
                sliderLabel.text = string.Format("{0}{1}", (int)displayValue, sliderUnit);
            else
                sliderLabel.text = string.Format("{0:0.0}{1}", displayValue, sliderUnit);
        }

        public float MaxValue
        {
            get => maxValue;
        }
        public float MinValue
        {
            get => minValue;
        }

        private void SetSmoothingTarget(float value)
        {
            syncedValue = value;
            targetValue = value;
            if (smoothRate <= 0 && reportedValue != syncedValue)
                ReportedValue = syncedValue;
            Updatelabel();
        }
        public float SyncedValue
        {
            get => syncedValue;
            set
            {
                if (!pointerIsDown)
                {
                    SetSmoothingTarget(value);
                    mySlider.SetValueWithoutNotify(syncedValue);
                }
            }
        }

        public float ReportedValue
        {
            get => reportedValue;
            private set
            {
                reportedValue = value;
                if (iHaveClientVar)
                {
                    /*
                    if (unitsInteger)
                        SliderClient.SetProgramVariable<int>(clientVariableName, Mathf.RoundToInt(reportedValue));
                    else
                        SliderClient.SetProgramVariable<Single>(clientVariableName, reportedValue);
                    */
                }
            }
        }
        public void onValue()
        {
            if (started)
            {
                SetSmoothingTarget(mySlider.value);
            }
            else
            {
                mySlider = GetComponent<Slider>();
                syncedValue = mySlider.value;
            }
        }

        public void ptrDn()
        {
            if (pointerIsDown)
                return;
            pointerIsDown = true;
            //if (iHaveClientPtr)
            //    SliderClient.SendCustomEvent(clientPtrEvent);
        }
        public void ptrUp()
        {
            pointerIsDown = false;
        }

        private float smthVel = 0;

        public void Update()
        {
            if (smoothRate <= 0f || reportedValue == targetValue)
                return;
            if (Mathf.Abs(reportedValue - targetValue) > smoothThreshold)
                ReportedValue = Mathf.SmoothDamp(reportedValue, targetValue, ref smthVel, 0.02f * smoothRate);
            else
                ReportedValue = targetValue;
        }

        private bool iHaveClientVar = false;
        private bool iHaveClientPtr = false;
        public void Start()
        {
            mySlider = GetComponent<Slider>();

            iHaveClientVar = (SliderClient != null) && (!string.IsNullOrEmpty(clientVariableName));
            iHaveClientPtr = (SliderClient != null) && (!string.IsNullOrEmpty(clientPtrEvent));

            if (sliderLabel != null && hideLabel)
                sliderLabel.text = "";

            mySlider.interactable = interactable;
            mySlider.minValue = minValue;
            mySlider.maxValue = maxValue;
            reportedValue = syncedValue;
            targetValue = syncedValue;
            SyncedValue = syncedValue;
            updateThreshold();
            started = true;
        }
    }
}
