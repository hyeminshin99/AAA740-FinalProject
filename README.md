# AAA740-FinalProject
23-2 Korea Univ. SPECIAL TOPICS IN ARTIFICIAL INTELLIGENCE Final Project
Meta-Transfer Learning for Super-resolution in Augmented Reality Domain

<p align="center"><img src="figure_1_ar.jpg" width="900"></p>

* 🌿 Overleaf link for Final Report
  https://www.overleaf.com/read/xztchtmqxhmk#8959bd
  
* 🎓 AR Image Dataset
  https://drive.google.com/drive/folders/17ggPCQdXcYRINPcuWdNTSwSzo4GEigSV?usp=sharing

* 🕸️ Online User Study Google Form
  https://forms.gle/cuEdQUCGid8kpitY7

* 🤓 User Study Result
  https://docs.google.com/spreadsheets/d/1RUWPdVnSepp1GDXtH5mDW0I5ENFYvjWqeOs0RlcAQmY/edit?usp=sharing


We conducted at a level of fine-tuning existing models. 

During this project, we aimed to gain a range of experience with as many models as possible and modify them in operational contexts.

Here are some of the models we came across during our project experience. 

_**MZSR, CMDSR, NatSR, Real-ESRGAN, MAML-SR (tried), MRDA (tried), MetaKernelGan (tried)**_

Among them, some models succeeded in producing SR results. Thus, these models are selected as comparison conditions (in the Related Works section).

Not only that, but we also successfully fine-tuned the MZSR model to enhance its performance on our AR Image dataset. 

As a result, we designate it as our own within this project.



## Related Work

### Super-Resolution Models

#### [NatSR (2019)] Natural and Realistic Single Image Super-Resolution with Explicit Natural Manifold Discrimination <a href="https://openaccess.thecvf.com/content_CVPR_2019/papers/Soh_Natural_and_Realistic_Single_Image_Super-Resolution_With_Explicit_Natural_Manifold_CVPR_2019_paper.pdf">Link</a>

#### [MZSR (2020)] Meta-Transfer Learning for Zero-Shot Super-Resolution <a href="https://openaccess.thecvf.com/content_CVPR_2020/papers/Soh_Meta-Transfer_Learning_for_Zero-Shot_Super-Resolution_CVPR_2020_paper.pdf">Link</a>

#### [Real-ESRGAN (2021)] Real-ESRGAN: Training Real-World Blind Super-Resolution with Pure Synthetic Data <a href="https://arxiv.org/abs/2107.10833">Link</a>

#### [CMDSR (2022)] Conditional Hyper-Network for Blind Super-Resolution with Multiple Degradations. <a href="https://ieeexplore.ieee.org/abstract/document/9785471">Link</a>

## AR Image Dataset

**Image Captured by Microsoft Hololens version 2**

<p align="center"><img src="hololens2.png" width="600"></p>

We prepared 959 images and 25 videos captured by HoloLens 2 (size: 1920 × 1080)

https://drive.google.com/drive/folders/17ggPCQdXcYRINPcuWdNTSwSzo4GEigSV?usp=sharing

## Experimental Results

**Results on various models for comparison**

<p align="center"><img src="results.jpg" width="900"></p>

**Results with User Study**

<p align="center"><img src="result_figure.png" width="550"></p>


