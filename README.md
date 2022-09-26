# Introduction
Unity have its own Third Person Controller package that can be acquired from the  [asset store](https://assetstore.unity.com/packages/essentials/starter-assets-third-person-character-controller-196526).
However, it lacks *in my opinion* a few touches to give an extra *feel* of a real third person controller.

## What do you mean?
I think that it is important to control not just your character but also the way you feed its Game Object some data.
Unity's Send Message just seems like a mess of layered complexity just to receive Inputs. What I did was separate the "new" Input System in a specific, independent script, so that it is easy to manipulate everything on the fly within the Inspector.

Not just that but also a way to interact with other Game Objects is a MUST in any kind of controller so I took the liberty of adding a few more scripts to handle that using C# `abstraction`!
>It is quite well commented in the Interactable.cs
## Dependencies
It was created within Unity Editor 2021.3.10f1 but it should work with:
- Unity Editor 2019.3 and above.
- Cinemachine
- (new) Input System
- Text Mesh PRO

## Disclaimer
The animations and model used were all imported from [mixamo](https://www.mixamo.com/#/).
