using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    public AudioSource firing;
    public AudioSource finished;

    public RawImage hitmarker;
    public AudioSource hitmarkerSound;

    private bool isOnTarget = false;

    void Start()
    {
        hitmarker.CrossFadeAlpha(0, 0f, true);
    }

    public void Overlap(bool isOverlapping)
    {
        isOnTarget = isOverlapping;

        if (isOnTarget)
        {
            Debug.Log("ON TARGET");
        } else
        {
            Debug.Log("OFF TARGET");
        }
    }

    async Task ShowHitmarker()
    {
        hitmarker.CrossFadeAlpha(1, 0.2f, false);
        await Task.Delay(200);
        hitmarker.CrossFadeAlpha(0, 0.2f, false);
    }

    async Task Shoot()
    {
        if (isOnTarget)
        {
            ShowHitmarker();
            hitmarkerSound.Play();
        }
        await Task.Delay(200);
    }

    async void Update()
    {
        /*
        if (Input.GetMouseButtonDown(0))
        {
            firing.loop = true;
            firing.Play();
        } else if (Input.GetMouseButtonUp(0))
        {
            firing.loop = false;
            finished.Play();
        }
        */

        if (Input.touchCount > 0)
        {
            Touch firstTouch = Input.GetTouch(0);
            if (firstTouch.phase == TouchPhase.Ended || firstTouch.phase == TouchPhase.Canceled)
            {
                firing.loop = false;
                finished.Play();
            }
            else if (firstTouch.phase == TouchPhase.Began)
            {
                firing.loop = true;
                firing.Play();

                Shoot();
            }
            else if (firstTouch.phase == TouchPhase.Began || firstTouch.phase == TouchPhase.Stationary || firstTouch.phase == TouchPhase.Moved)
            {
                Shoot();
            }
        }
    }
}
