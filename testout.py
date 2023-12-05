import torch
import torchvision.transforms as transforms
from PIL import Image
import matplotlib.pyplot as plt

# 모델 정의
model = MAMLSR() # 모델 구조를 정의
model.load_state_dict(torch.load('C:/Users/ShinHyemin/VisualStudioProject/MAML-SR/final_model_sisrx4.pth'))
model.eval() # 평가 모드로 설정

# 이미지 불러오기 및 전처리
image_path = 'C:/Users/ShinHyemin/VisualStudioProject/MAML-SR/input/atest.png'
input_image = Image.open(image_path)#.convert('RGB')
#plt.imshow(input_image)

# 이미지를 모델에 맞게 전처리
transform = transforms.Compose([
    #transforms.Resize((720, 1280)),  # 예시 크기, 실제 모델에 맞게 조정 필요 86, 86
    transforms.ToTensor(),
    #transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]) #(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
])
input_tensor = transform(input_image).unsqueeze(0)  # 배치 차원 추가

# 모델을 사용하여 슈퍼-레졸루션 이미지 생성
with torch.no_grad():
    output_tensor = model(input_tensor)

# output_tensor는 tuple임.
# 텐서를 이미지로 변환
output_image = output_tensor[0].squeeze().permute(1, 2, 0).numpy() #
output_image = (output_image * 255).clip(0, 255).astype('uint8')

# 결과 이미지 시각화
plt.imshow(output_image)
plt.title('Super-Resolution Image')
plt.show()