using UnityEngine;
namespace AdrianWez
{
    [RequireComponent(typeof(Inputs))]
    public class PlayerInteractions : MonoBehaviour
    {
        [Header("Interactions")]
        [SerializeField] private Transform[] _rayPoints;                        // transforms to be used for casting rays and detect interactable objects
        [SerializeField] private float _interactionDistance = 1.5f;             // distance between player and interatable object to trigger the process
        [SerializeField] private TMPro.TMP_Text _interactionText;               // UI to display returned discription from interactable object
        [SerializeField] private UnityEngine.UI.Image _interactionHoldProgress; // UI progress bar to display passage of time while holding the interaction key
        private Inputs _input { get => GetComponent<Inputs>(); }                // The Inputs component attached to the object
        private void Update()
        {
                // using transoforms in the array to check for interactable objects
                foreach (Transform _rayPoint in _rayPoints)
                {
                    if (Physics.Raycast(_rayPoint.position, transform.forward, out RaycastHit _hit, _interactionDistance) && _hit.collider.GetComponent<Interactable>())
                    {
                        Interactable _interactable = _hit.collider.GetComponent<Interactable>();

                        _interactionText.text = _interactable.GetDescription();
                        _interactionHoldProgress.transform.parent.gameObject.SetActive(_interactable._interactionType == Interactable.InteractionType.Hold);

                        // interacting accordingly with the interactable found
                        switch (_interactable._interactionType)
                        {
                            case Interactable.InteractionType.Click:
                                if (_input._interact.IsPressed()) _interactable.Interact();
                                break;
                            case Interactable.InteractionType.Hold:
                                if (_input._interact.IsInProgress())
                                {
                                    _interactable.IncreaseHoldTime();
                                    if (_interactable.GetHoldTime() > 1f)
                                    {
                                        _interactable.Interact();
                                        _interactable.ResetHoldTime();
                                    }
                                }
                                else _interactable.ResetHoldTime();
                                _interactionHoldProgress.fillAmount = _interactable.GetHoldTime();
                                break;
                        }
                        break;
                    }
                    else
                    {
                        // no interactable object in range
                        _interactionText.text = "";
                        _interactionHoldProgress.transform.parent.gameObject.SetActive(false);
                    }
                }
        }
    }
}