#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
HisAiCli.py
===========
Công cụ dòng lệnh (CLI) gọi AI đa mô hình OpenRouter phục vụ nghiệp vụ Bệnh viện Bạch Mai.
Tự động sử dụng mô hình Free Tier (stealth/ox-alpha, minimax-m3, gemma-4, nemotron-3-ultra...).

Cú pháp:
    python HisAiCli.py ask "Triệu chứng và chẩn đoán phân biệt..."
    python HisAiCli.py ocr "C:\path\to\image.jpg" --task glucose
    python HisAiCli.py json "Trích xuất danh sách thuốc: Paracetamol 500mg x 2v..."
    python HisAiCli.py models
"""


import sys
import os
import argparse
import json
from openrouter_client import (
    generate_with_fallback,
    extract_json_structured,
    get_openrouter_api_key,
    FREE_VISION_MODELS,
    FREE_REASONING_MODELS,
    FREE_CODE_MODELS
)

# Đảm bảo UTF-8 chuẩn trên Windows console
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass


def cmd_ask(args):
    prompt = " ".join(args.prompt) if isinstance(args.prompt, list) else args.prompt
    system_prompt = (
        "Bạn là trợ lý AI y tế chuyên sâu hỗ trợ bác sĩ lâm sàng tại Bệnh viện Bạch Mai "
        "(Khoa Chấn thương Chỉnh hình & Cột sống). Hãy trả lời ngắn gọn, chuẩn xác, "
        "bám sát thực tiễn điều trị và phác đồ chuẩn."
    )
    messages = [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": prompt}
    ]
    models = [args.model] if args.model else FREE_REASONING_MODELS
    print(f"\n🧠 Đang xử lý câu hỏi lâm sàng...")
    content, used_model = generate_with_fallback(messages, model_candidates=models, verbose=True)
    print(f"\n" + "=" * 80)
    print(f"📋 PHẢN HỒI LÂM SÀNG (Mô hình: {used_model})")
    print("=" * 80)
    print(content)
    print("=" * 80 + "\n")


def cmd_ocr(args):
    image_path = args.image
    task = args.task
    if task == "glucose":
        from parse_glucose_image import OCR_PROMPT
        prompt = OCR_PROMPT
    else:
        prompt = (
            "Hãy đọc và trích xuất toàn bộ thông tin chi tiết từ hình ảnh y tế/phiếu kết quả này. "
            "Nếu là phim CĐHA (X-quang, CT, MRI), bắt buộc nêu đích danh từng tầng giải phẫu và mức độ tổn thương cấp/cũ."
        )

    print(f"\n🔍 Đang phân tích hình ảnh: {image_path} (Nhiệm vụ: {task})...")
    data = extract_json_structured(prompt=prompt, image_path=image_path)
    print("\n" + "=" * 80)
    print("📊 KẾT QUẢ TRÍCH XUẤT JSON")
    print("=" * 80)
    print(json.dumps(data, ensure_ascii=False, indent=2))
    print("=" * 80 + "\n")

    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print(f"✅ Đã lưu kết quả ra: {args.output}")


def cmd_json(args):
    prompt = " ".join(args.prompt) if isinstance(args.prompt, list) else args.prompt
    system_prompt = "You are a specialized clinical data parser. Output valid JSON only."
    print(f"\n🧩 Đang trích xuất JSON cấu trúc...")
    data = extract_json_structured(prompt=prompt, system_prompt=system_prompt)
    print("\n" + "=" * 80)
    print("📊 KẾT QUẢ JSON")
    print("=" * 80)
    print(json.dumps(data, ensure_ascii=False, indent=2))
    print("=" * 80 + "\n")


def cmd_models(args):
    print("\n" + "=" * 80)
    print("🌐 DANH MỤC MÔ HÌNH FREE TIER OPENROUTER ĐÃ ĐƯỢC CẤU HÌNH")
    print("=" * 80)
    print("\n1. Nhóm Đa phương thức (Vision/OCR/Images):")
    for m in FREE_VISION_MODELS:
        print(f"   - {m}")

    print("\n2. Nhóm Lập luận Bệnh án & Tóm tắt (Reasoning/Text):")
    for m in FREE_REASONING_MODELS:
        print(f"   - {m}")

    print("\n3. Nhóm Lập trình & Trích xuất JSON (Code/Structured):")
    for m in FREE_CODE_MODELS:
        print(f"   - {m}")
    print("=" * 80 + "\n")


def main():
    parser = argparse.ArgumentParser(description="HIS OpenRouter AI Command Line Interface")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Lệnh ask
    p_ask = subparsers.add_parser("ask", help="Đặt câu hỏi lâm sàng / tóm tắt văn bản")
    p_ask.add_argument("prompt", nargs="+", help="Nội dung câu hỏi")
    p_ask.add_argument("--model", "-m", default="", help="Chỉ định mô hình cụ thể")
    p_ask.set_defaults(func=cmd_ask)

    # Lệnh ocr
    p_ocr = subparsers.add_parser("ocr", help="Phân tích ảnh / phiếu kết quả")
    p_ocr.add_argument("image", help="Đường dẫn file ảnh (JPG/PNG/PDF)")
    p_ocr.add_argument("--task", "-t", choices=["glucose", "xquang", "general"], default="general", help="Loại nhiệm vụ OCR")
    p_ocr.add_argument("--output", "-o", default="", help="File JSON đầu ra")
    p_ocr.set_defaults(func=cmd_ocr)

    # Lệnh json
    p_json = subparsers.add_parser("json", help="Trích xuất dữ liệu có cấu trúc JSON từ văn bản")
    p_json.add_argument("prompt", nargs="+", help="Văn bản cần trích xuất")
    p_json.set_defaults(func=cmd_json)

    # Lệnh models
    p_models = subparsers.add_parser("models", help="Xem danh mục mô hình miễn phí đã cấu hình")
    p_models.set_defaults(func=cmd_models)

    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
