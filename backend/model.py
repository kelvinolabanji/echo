from transformers import CLIPProcessor, CLIPModel
import os

MODEL_PATH = os.path.join(os.path.dirname(__file__), "models", "clip")

print("Loading CLIP model...")
model = CLIPModel.from_pretrained(MODEL_PATH)
processor = CLIPProcessor.from_pretrained(MODEL_PATH)
model.eval()
print("Model loaded.")