from torch.utils.data import Dataset
import numpy as np
import torch
from torch import nn
from torchvision import transforms as T, models
from collections import OrderedDict
import segmentation_models_pytorch as smp
from fastapi.responses import StreamingResponse
import pydicom as dicom
import pydicom
from fastapi.middleware.cors import CORSMiddleware
from fastapi import FastAPI, File, UploadFile
from PIL import Image
import io
import base64
from fastapi.responses import JSONResponse
import traceback
from torchvision import transforms



# Initialize ResNet101 model for pneumonia classification
class ResNetPNImageData:
    def __init__(self):
        print("iamge_transform")
        self.transform = transforms.Compose([
            transforms.Resize(256),
            transforms.CenterCrop(224),
            transforms.ToTensor(),
            transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        ])

    def process_image(self, image):
        print("iamge_transform_process")
        # Ensure the image is in RGB format
        if image.mode != 'RGB':
            image = image.convert('RGB')
        return self.transform(image)

    def process_dicom(self, dicom_data):
        # Load DICOM file
        dicom_image = pydicom.dcmread(io.BytesIO(dicom_data))
        # Convert to numpy array and then to PIL image
        image_array = dicom_image.pixel_array
        image = Image.fromarray(image_array).convert('RGB')
        return self.process_image(image)

class resnet_pn():
    def __init__(self) -> None:
        
        self.cuda_available()
        self.load_model()
        self.lung_class = {0 : "Normal", 1 : "Pneumonia"}

    def load_model(self):
        print("loadmodel")
        self.resnet_model = models.resnet101()
        num_ftrs = self.resnet_model.fc.in_features
        self.resnet_model.fc = nn.Sequential(
            nn.Linear(num_ftrs, 128),
            nn.ReLU(inplace=True),
            nn.Linear(128, 2)
        )
        self.resnet_model.load_state_dict(torch.load('model_code/complete_model_resnet101.pth', map_location=self.device))
        self.resnet_model.to(self.device).eval()
               
    def cuda_available(self):
        self.device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

    def analyze_image_judgePN(self, image: Image.Image):
        print("analyze_image")
        data = ResNetPNImageData()
        transform_img = data.process_image(image)
        with torch.no_grad():
            pred = self.resnet_model(transform_img.unsqueeze(0).to(self.device))
            probabilities = torch.nn.functional.softmax(pred, dim=1)
            confidence, preds = torch.max(probabilities, 1)
            class_name = self.lung_class[preds.item()]
        return {"result": class_name, "confidence": round(confidence.item(),4)}
