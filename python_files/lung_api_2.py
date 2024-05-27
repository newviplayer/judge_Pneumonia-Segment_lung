from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
from PIL import Image
import io
import numpy as np
from fastapi.responses import StreamingResponse
import pydicom as dicom
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
from PIL import Image
import io
import os  
import model_code.my_vgg16 as my_vgg16
import model_code.my_seg as my_seg
import model_code.my_resnet as my_resnet
import model_code.my_alexnet as my_alexnet
import uvicorn

os.environ["CUDA_DEVICE_ORDER"]="PCI_BUS_ID"  # Arrange GPU devices starting from 0
os.environ["CUDA_VISIBLE_DEVICES"]= "1"  # Set the GPU 2 to use

# judge_pn_models
vgg_model = my_vgg16.vgg16_pn()
resnet_model = my_resnet.resnet_pn()
alexnet_model = my_alexnet.alexnet_pn()
# seg_model
seg_model = my_seg.my_seg_model()

app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 모든 출처 허용, 실제 배포 시에는 보안을 위해 구체적인 출처 지정
    allow_credentials=True,
    allow_methods=["*"],  # 모든 HTTP 메소드 허용
    allow_headers=["*"],  # 모든 HTTP 헤더 허용
)

@app.post("/judgePN-fromimg-with-vgg16/")
async def analyze_vgg_image_route(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))
        result = vgg_model.analyze_image_judgePN(image)
        return result
    except Exception as e:
        return JSONResponse(status_code=500, content={"error": str(e)})

@app.post("/judgePN-fromimg-with-resnet101/")
async def analyze_resnet_image_route(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))
        result = resnet_model.analyze_image_judgePN(image)
        return result
    except Exception as e:
        return JSONResponse(status_code=500, content={"error": str(e)})
    
@app.post("/judgePN-fromimg-with-alexnet/")
async def analyze_alexnet_image_route(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))
        result = alexnet_model.analyze_image_judgePN(image)
        return result
    except Exception as e:
        return JSONResponse(status_code=500, content={"error": str(e)})
    
@app.post("/lung-image-mask/")
async def convert_to_mask(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))
        mask_data = seg_model.create_mask(image)
        return StreamingResponse(io.BytesIO(mask_data), media_type="image/jpeg")
    except Exception as e:
        return {"error": str(e)}

# ============================get-dicom===========================================

@app.post("/process_dicom_file/")
async def process_dicom_file(file: UploadFile = File(...)):
    try:
        dcm = dicom.dcmread(file.file)
        img_data = dcm.pixel_array
        output = (img_data - np.min(img_data)) / (np.max(img_data) - np.min(img_data))
        output = np.array((output * 255), dtype=np.uint8)
        dcm_img = Image.fromarray(output)
        output_buffer = io.BytesIO()
        dcm_img.save(output_buffer, format="JPEG")
        return StreamingResponse(io.BytesIO(output_buffer.getvalue()), media_type="image/jpeg")

    except Exception as e:
        return {"error": str(e)}


# ============================get-dicom===========================================