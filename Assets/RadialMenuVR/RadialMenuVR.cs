using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;

namespace VR
{
    public class RadialMenuVR : MonoBehaviour
    {
        [SerializeField] public float radius = 5; //the radius of the circle that contains the buttons, the larger the more spaced out the buttons will be
        [SerializeField] public List<RadialMenuEntry> entries; 
        [SerializeField] public RectTransform radialMenuRectTransform;
        [SerializeField] public Transform rightHandTransform;
        [SerializeField] public Vector3 menuPosOffset;
        [SerializeField] public InputActionReference toggleMenuOpenRef;
        [SerializeField] public InputActionReference holdRotateMenuRef;

        [SerializeField] public GameObject menuArrow;
        [SerializeField] public GameObject button;

        private List<GameObject> Buttons;
        private Dictionary<float, string> ButtonsAngles;
        private bool isOpen;
        private bool isRotationActive;
        private bool canRotateMenu;
        private float lastAngle;
        private bool isInAnimation;
        private float animationSpeed;
        private int menuId; //to indentify wich type of rotation is being used. 0- rotation of all buttons with the rotation of the wrist
                            //                                                  1- rotation of only the arrow
                            //                                                  2- rotation of the button in the direction of the wrist rotation in a continuous motion
                            //                                                  3- rotation one by one of the button, similar to disk a phone number on a older telephone
        private float buttonScale; //calculated with the number of buttons, to change the min and max values see Rearrange function
        private List<Vector3> currButtonsPositions;
        private GameObject buttonToPaint;
        private matrixClass matrixBezier;

    public void Awake()
        {
            toggleMenuOpenRef.action.started += ToggleMenuOpen;
            holdRotateMenuRef.action.started += HoldRotateMenu;
            holdRotateMenuRef.action.canceled += HoldRotateMenu;
        }

        public void OnDestroy()
        {
            toggleMenuOpenRef.action.started -= ToggleMenuOpen;
            holdRotateMenuRef.action.started -= HoldRotateMenu;
            holdRotateMenuRef.action.canceled -= HoldRotateMenu;
        }

        public void Start()
        {
            Buttons = new List<GameObject>();
            ButtonsAngles = new Dictionary<float, string>();
            currButtonsPositions = new List<Vector3>();
            radialMenuRectTransform.SetPositionAndRotation(rightHandTransform.position + menuPosOffset, rightHandTransform.rotation);
            isOpen = false;
            isRotationActive = false;
            canRotateMenu = true;
            lastAngle = 0;
            menuId = 2;
            matrixBezier = new matrixClass(4);


        }

        public IEnumerator MoveAnchoPos(GameObject obj, Vector3 pos, bool dir, float vel, bool canFinishAnim, bool destroyMenu)
        {

            float objX = obj.transform.localPosition.x;
            float objY = obj.transform.localPosition.y;
            float posX = pos.x;
            float posY = pos.y;

            Vector3 p1 = obj.transform.localPosition;
            Vector3 p2;
            if (!dir)
            {
                p2 = new Vector3((((1 / 2) * (posX + objX)) - ((float)(Math.Sqrt(3) / 2) * (posY - objY))) * 3,
                                     (((1 / 2) * (posY + objY)) + ((float)(Math.Sqrt(3) / 2) * (posX - objX))) * 3, 0);
            }
            else
            {
                p2 = new Vector3((((1 / 2) * (posX + objX)) + ((float)(Math.Sqrt(3) / 2) * (posY - objY))) * 3,
                                     (((1 / 2) * (posY + objY)) - ((float)(Math.Sqrt(3) / 2) * (posX - objX))) * 3, 0);
            }

            float t = 0.0f;
            while (t < 1.0f)
            {
                float pX = (1 - t) * (1 - t) * p1.x + 2 * (1 - t) * t * p2.x + t * t * pos.x;
                float pY = (1 - t) * (1 - t) * p1.y + 2 * (1 - t) * t * p2.y + t * t * pos.y;
 
                obj.transform.SetLocalPositionAndRotation(new Vector3(pX, pY, 0), new Quaternion(0, 0, 0, 0));
                t += Time.deltaTime * vel;

                yield return null;
            }

            obj.transform.SetLocalPositionAndRotation(pos, new Quaternion(0, 0, 0, 0));
            if (canFinishAnim)
                AnimationFinished();
            if (destroyMenu)
                DestroyMenu();
        }

        public IEnumerator Scale(GameObject obj, float upScale, float duration, bool canFinishAnim)
        {
            Vector3 initialScale = obj.transform.localScale;

            for (float time = 0; time < duration * 2; time += Time.deltaTime * 2)
            {
                obj.transform.localScale = Vector3.Lerp(initialScale, new Vector3(upScale, upScale, 1), time);
                yield return null;
            }
            if (canFinishAnim)
                AnimationFinished();
        }

        public void Update()
        {
            if (!isRotationActive)
            {
                transform.position = rightHandTransform.position;
                menuArrow.transform.position = transform.position;
                transform.rotation = rightHandTransform.rotation;
                menuArrow.transform.rotation = transform.rotation;
            }
            else
            {
                transform.position = rightHandTransform.position;
                menuArrow.transform.position = transform.position;
                CurrentMenuRender();

            }

            if ((rightHandTransform.rotation.eulerAngles.z > 0 && rightHandTransform.rotation.eulerAngles.z < 4) || (rightHandTransform.rotation.eulerAngles.z > -6 && rightHandTransform.rotation.eulerAngles.z < 0))
            {
                canRotateMenu = true;
            }
        }

        

        public void CurrentMenuRender()
        {
            if(menuId == 0)
            {
                    animationSpeed = .1f;
                    RotateMenuByAngle(rightHandTransform.rotation.eulerAngles.z, false);
                
            }
            else if(menuId == 1)
            {
                if ((rightHandTransform.rotation.eulerAngles.z > 2 && rightHandTransform.rotation.eulerAngles.z < 358))
                {
                    animationSpeed = .0001f;
                    RotateMenuByAngle(rightHandTransform.rotation.eulerAngles.z, true);
                }
            }
            else if (menuId == 2)
            {
                if ((rightHandTransform.rotation.eulerAngles.z > 10 && rightHandTransform.rotation.eulerAngles.z < 350) && !isInAnimation)
                {
                    animationSpeed = 1.6f;
                    RotateMenuByDirection(rightHandTransform.rotation.eulerAngles.z);
                }
            }
            else if(menuId == 3)
            {
                animationSpeed = .4f;
                if ((rightHandTransform.rotation.eulerAngles.z > 10 && rightHandTransform.rotation.eulerAngles.z < 350) && !isInAnimation)
                {
                    RotateMenuOneByOne(rightHandTransform.rotation.eulerAngles.z);
                }
            }
        }

        public void ToggleMenuType(InputAction.CallbackContext context)
        {
            if(menuId == 3)
            {
                menuId=0;
                return;
            }
            menuId++;
        }

        private void ToggleMenuOpen(InputAction.CallbackContext context)
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void HoldRotateMenu(InputAction.CallbackContext context)
        {
            if (!isOpen)
            {
                return;
            }
            List<Image> icons = Buttons[0].GetComponentsInChildren<Image>(true).ToList<Image>();
            if (isRotationActive)
            {
                if (menuId != 0)
                    icons.First(s => s.name == "Seta").gameObject.SetActive(false);
                else
                    menuArrow.SetActive(false);
                isRotationActive = false;
                lastAngle = 0;

                if (menuId != 0)
                    Buttons[0].GetComponent<Button>().onClick.Invoke();
                else
                    buttonToPaint.GetComponent<Button>().onClick.Invoke();
            }
            else
            {
                if(menuId!=0)
                    icons.First(s => s.name == "Seta").gameObject.SetActive(true);
                else
                    menuArrow.SetActive(true);
                isRotationActive = true;
                
            }
        }

        private void RotateMenuOneByOne(float handAngle)
        {
            //Checa se pode rodar
            if (!canRotateMenu) return;
            RotateMenuByDirection(handAngle);
            canRotateMenu = false;
        }
        private void AnimationFinished()
        {
            isInAnimation = false;
        }
        private void updateAllButtonsPos()
        {
            currButtonsPositions.Clear();
            foreach (var b in Buttons)
            {
                currButtonsPositions.Add(b.gameObject.transform.localPosition);
            }
        }

        private void RotateButtonsByAngleLeft(bool highlightMode)
        {
            Debug.Log("Entro esq");
            //Rotate
            RectTransform currButtonToRotate;
            updateAllButtonsPos();

            //Remove indicator
            List<Image> icons = Buttons[0].GetComponentsInChildren<Image>(true).ToList<Image>();
            icons.First(s => s.name == "Seta").gameObject.SetActive(false);
            //Sort with new position

            int lastIndex = Buttons.Count - 1;
            var lastButton = Buttons[lastIndex];
            var lastPos = currButtonsPositions[lastIndex];
            Buttons.RemoveAt(lastIndex);
            Buttons.Insert(0, lastButton);
            currButtonsPositions.RemoveAt(lastIndex);
            currButtonsPositions.Insert(0, lastPos);
            icons = Buttons[0].GetComponentsInChildren<Image>(true).ToList<Image>();
            icons.First(s => s.name == "Seta").gameObject.SetActive(true);
            //Put back indicator
            if (!highlightMode)
            {
               
                isInAnimation = true;
                for (int i = 0; i < Buttons.Count - 1; i++)
                {
                    StartCoroutine(MoveAnchoPos(Buttons[i], currButtonsPositions[i + 1], false, animationSpeed, false,false));
                    StartCoroutine(Scale(Buttons[i], buttonScale, 0.6f, false));
                }
                StartCoroutine(MoveAnchoPos(Buttons[Buttons.Count - 1], currButtonsPositions[0], false, animationSpeed, false,false));
                StartCoroutine(Scale(Buttons[Buttons.Count - 1], buttonScale, 1.5f, true));
   

            }
            updateAllButtonsPos();
        }

        private void RotateButtonsByAngleRight(bool highlightMode)
        {
            Debug.Log("Entro dir");
            //Rotate
            RectTransform currButtonToRotate;
            updateAllButtonsPos();

            //Remove indicator
            List<Image> icons = Buttons[0].GetComponentsInChildren<Image>(true).ToList<Image>();
            icons.First(s => s.name == "Seta").gameObject.SetActive(false);
            //Sort with new position
            var auxButton = Buttons[0];
            var auxPos = currButtonsPositions[0];
            Buttons.Add(auxButton);
            Buttons.RemoveAt(0);
            currButtonsPositions.Add(auxPos);
            currButtonsPositions.RemoveAt(0);
            icons = Buttons[0].GetComponentsInChildren<Image>(true).ToList<Image>();
            icons.First(s => s.name == "Seta").gameObject.SetActive(true);
            //Put back indicator
            if (!highlightMode)
            {
                
                isInAnimation = true;
                for (int i = Buttons.Count - 1; i > 0; i--)
                {
                    StartCoroutine(MoveAnchoPos(Buttons[i], currButtonsPositions[i - 1], true, animationSpeed, false, false));
                    StartCoroutine(Scale(Buttons[i], buttonScale, 0.6f, false));
                }
                StartCoroutine(MoveAnchoPos(Buttons[0], currButtonsPositions[Buttons.Count - 1], true, animationSpeed, false,false));
                StartCoroutine(Scale(Buttons[Buttons.Count - 1], buttonScale, 1.5f, true)); // the animation speed of this needs to be more slower to give time for all other animations complete

            }
            updateAllButtonsPos();
        }

        private GameObject FindClosestButton()
        {
            GameObject closestButton = Buttons[0];
            float closestDistance = float.MaxValue;
            GameObject arrow = menuArrow.GetComponentInChildren<Image>().gameObject;

            foreach (GameObject b in Buttons)
            {
                float distance = Vector3.Distance(arrow.transform.position, b.transform.position);
                StartCoroutine(Scale(b, buttonScale, 1f, false));
           
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestButton = b;
                   
                }
            }
            StartCoroutine(Scale(closestButton, buttonScale + .1f, 1f, false));
            return closestButton;
        }

        private void RotateMenuByAngle(float handAngle, bool highlightMode)
        {
            
            if (!highlightMode)
            {
                float adjustedAngle = handAngle * 2;

                if (adjustedAngle > 360)
                    adjustedAngle -= 360;
                else if (adjustedAngle < 0)
                    adjustedAngle += 360;

                float currentAngle = transform.rotation.eulerAngles.z;

                float angleDiff = adjustedAngle - currentAngle;
                float shortestAngleDiff = Mathf.DeltaAngle(currentAngle, adjustedAngle);

                float direction = Mathf.Sign(shortestAngleDiff);


                if (shortestAngleDiff != 0)
                {
                    transform.Rotate(new Vector3(0, 0, 1), direction * Mathf.Min(Mathf.Abs(angleDiff), 3.0f));
                    foreach (GameObject b in Buttons)
                    {
                        b.transform.Rotate(new Vector3(0, 0, 1), -direction * Mathf.Min(Mathf.Abs(angleDiff), 3.0f));

                    }
                }
                buttonToPaint = FindClosestButton();
                Debug.Log("Z angle: " + transform.rotation.eulerAngles.z + "Hand angle: " + handAngle * 2);
            }
            else
            {
                string nameButtonDest = findButtonNameByAngle(handAngle);
                while (Buttons[0].GetComponentInChildren<TextMeshProUGUI>().text != nameButtonDest)
                {

                    if (handAngle < lastAngle)
                    {
                        if (lastAngle - handAngle <= 180)
                        {

                            RotateButtonsByAngleRight(highlightMode);
                        }
                        else
                        {

                            RotateButtonsByAngleLeft(highlightMode);
                        }
                    }
                    else if (handAngle > lastAngle)
                    {
                        if (handAngle - lastAngle <= 180)
                        {

                            RotateButtonsByAngleLeft(highlightMode);
                        }
                        else
                        {

                            RotateButtonsByAngleRight(highlightMode);
                        }
                    }

                }
                lastAngle = handAngle;
            }
            
        }

        private void RotateMenuByDirection(float handAngle)
        {
            
            if (handAngle > 180)
            {
                RotateButtonsByAngleRight(false);
            }
            else if (handAngle < 180)
            {
               
                RotateButtonsByAngleLeft(false);
            }
            
            lastAngle = handAngle;
        }

        private string findButtonNameByAngle(float handAngle)
        {
            var allKeys = ButtonsAngles.Keys;
            var adjustedAngle = handAngle * 2;

            if (adjustedAngle > 360)
                adjustedAngle -= 360;
            else if(adjustedAngle<0)
                adjustedAngle += 360;


            float closestDifference = float.MaxValue;
            string closestButtonName = string.Empty;

            foreach (float key in allKeys)
            {
                float angleDifference = Mathf.Abs(adjustedAngle - key);
                if (angleDifference < closestDifference)
                {
                    closestDifference = angleDifference;
                    closestButtonName = ButtonsAngles[key];
                }
            }

            foreach(GameObject b in Buttons)
            {
                if(closestButtonName == b.GetComponentInChildren<TextMeshProUGUI>().text)
                {
                    StartCoroutine(Scale(b, buttonScale + .1f, 1f, false));
                    
                }
                else
                {
                    StartCoroutine(Scale(b, buttonScale, 1f, false));
                }
            }

            return closestButtonName;
        }

        public void SpawnEntries()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                GameObject instantiatedButton = Instantiate(button, transform);
                instantiatedButton.GetComponent<Button>().onClick.AddListener(entries[i].uEvent.Invoke);
                instantiatedButton.GetComponentInChildren<TextMeshProUGUI>().text = entries[i].label;
                List<Image> icons = instantiatedButton.GetComponentsInChildren<Image>().ToList<Image>();
                icons.First(s => s.name == "Icone").sprite = entries[i].icon;
                Buttons.Add(instantiatedButton);
            }

        }
        
       
        public void Open()
        {
            SpawnEntries();
            Rearrange();
            isOpen = true;
            Buttons[0].SetActive(true);
            Buttons[0].GetComponent<Button>().Select();
        }

        public void Close()
        {
            for (int i = 0; i < Buttons.Count-1; i++)
            {

                GameObject currButton = Buttons[i].gameObject;
                StartCoroutine(MoveAnchoPos(Buttons[i], Vector3.zero, false, 1f, true,false));
                                                                                             
 
                                                                                            
            }
            StartCoroutine(MoveAnchoPos(Buttons[Buttons.Count - 1], Vector3.zero, false, 1f, true,true));

            Buttons.Clear();
            ButtonsAngles.Clear();
            isOpen = false;
        }
        public void DestroyMenu()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void Rearrange()
        {
            float radiansOfSeparation = (Mathf.PI * 2) / entries.Count;
            float angle_increment = 360 / Buttons.Count;


            float minScale = 0.1f; // Minimum scale factor
            float maxScale = 0.5f; // Maximum scale factor
            float x, y;

            for (int i = 0; i < Buttons.Count; i++)
            {
                if(i == 0)
                {
                    ButtonsAngles.Add(angle_increment-5, Buttons[(Buttons.Count - 1) - i].GetComponentInChildren<TextMeshProUGUI>().text);
                }
                else
                {
                    ButtonsAngles.Add(angle_increment * i, Buttons[(Buttons.Count - 1) - i].GetComponentInChildren<TextMeshProUGUI>().text);
                }
                
                
                
                x = Mathf.Sin(radiansOfSeparation * i) * radius;
                y = Mathf.Cos(radiansOfSeparation * i) * radius;
              
                // Calculate the distance between buttons (assuming a circular shape)
                float distance = radius * radiansOfSeparation;

                // Calculate the scale factor based on the distance
                buttonScale = Mathf.Lerp(minScale, maxScale, distance / radius);

                StartCoroutine(MoveAnchoPos(Buttons[i], new Vector3(x, y, 0), false, 1f, false,false));
                StartCoroutine(Scale(Buttons[i], buttonScale, 0.6f, false));
                
            }
        }
    }
}

