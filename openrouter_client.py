#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
openrouter_client.py
====================
Bộ điều phối đa mô hình AI OpenRouter chuẩn hóa cho Hệ Thống Tích Hợp HIS/EMR.
Hỗ trợ cơ chế tự động chuyển tầng dự phòng (Multi-Tier Smart Fallback) 100% Free Tier:
  - Tier 1 (Vua Đa phương thức & Suy luận 1M Context): stealth/ox-alpha
  - Tier 2 (Dự phòng Đa phương thức 1M Context): minimax/minimax-m3:free
  - Tier 3 (Dự phòng Đa phương thức 256K Context Google): google/gemma-4-31b-it:free
  - Tier 4 (Siêu mô hình Suy luận 550B MoE 1M Context): nvidia/nemotron-3-ultra-550b-a55b:free
  - Tier 5 (Chuyên sâu Lập trình & Logic 256K Context): cohere/north-mini-code:free
  - Tier 6 (Mô hình Ngôn ngữ Lớn 256K Context): z-ai/glm-5.2:free
  - Tier 7 (Bộ định tuyến Ngẫu nhiên Miễn phí): openrouter/free
"""

import sys
import os
import json
import base64
import re
import urllib.request
import urllib.error
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple

# Đảm bảo UTF-8 chuẩn trên Windows console
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

# ==============================================================================
# DANH MỤC CÁC MÔ HÌNH FREE TIER TRÊN OPENROUTER (CẬP NHẬT 2026)
# ==============================================================================

# Mô hình ưu tiên cho tác vụ Đa phương thức (Vision/OCR/Image/PDF)
FREE_VISION_MODELS = [
    "minimax/minimax-m3:free",          # 1M context, $0, Multimodal + Strict JSON + Tiếng Việt chuẩn
    "google/gemma-4-31b-it:free",       # 256K context, $0, Google Multimodal
    "google/gemma-4-26b-a4b-it:free",   # 256K context, $0, Fast Multimodal
    "openrouter/free"                   # Auto fallback router
]

# Mô hình ưu tiên cho tác vụ Lập luận Bệnh án & Tóm tắt Văn bản Dài (Reasoning/Text)
FREE_REASONING_MODELS = [
    "minimax/minimax-m3:free",                  # 1M context, $0, Deep CoT + Medical Reasoning
    "nvidia/nemotron-3.5-lightning:free",       # 1M context, $0, Fast Reasoning
    "z-ai/glm-5.2:free",                        # 256K context, $0, Deep Reasoning
    "openrouter/free"                           # Auto router
]

# Mô hình ưu tiên cho Lập trình / Viết Script / JSON Cấu trúc (Code/Structured)
FREE_CODE_MODELS = [
    "minimax/minimax-m3:free",          # 1M context, $0, Strict JSON output
    "poolside/laguna-s-2.1:free",       # 262K context, $0, Code & Logic
    "z-ai/glm-5.2:free",                # 256K context, $0, Structured data
    "openrouter/free"
]


def get_openrouter_api_key() -> str:
    """Lấy OpenRouter API key từ env hoặc Windows Registry"""
    key = os.environ.get("OPENROUTER_API_KEY", "").strip()
    if not key and sys.platform == "win32":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as reg_key:
                val, _ = winreg.QueryValueEx(reg_key, "OPENROUTER_API_KEY")
                key = str(val).strip()
        except Exception:
            pass
    return key


def load_file_base64(file_path: str) -> Tuple[str, str]:
    """Đọc file ảnh/PDF và trả về (base64_data, mime_type)"""
    p = Path(file_path)
    if not p.exists():
        raise FileNotFoundError(f"Không tìm thấy file: {file_path}")

    ext = p.suffix.lower()
    mime_map = {
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".png": "image/png",
        ".gif": "image/gif",
        ".webp": "image/webp",
        ".bmp": "image/bmp",
        ".pdf": "application/pdf",
    }
    mime = mime_map.get(ext, "image/jpeg")

    with open(file_path, "rb") as f:
        data = base64.b64encode(f.read()).decode("utf-8")
    return data, mime


def call_openrouter_raw(
    messages: List[Dict[str, Any]],
    model: str = "stealth/ox-alpha",
    api_key: Optional[str] = None,
    response_format: Optional[Dict[str, str]] = None,
    max_tokens: int = 16384,
    temperature: float = 0.2,
    timeout: int = 90
) -> Dict[str, Any]:
    """Gửi HTTP request trực tiếp tới OpenRouter API"""
    key = api_key or get_openrouter_api_key()
    if not key:
        raise ValueError("Chưa thiết lập OPENROUTER_API_KEY (trong env hoặc Windows Registry)!")

    url = "https://openrouter.ai/api/v1/chat/completions"
    headers = {
        "Authorization": f"Bearer {key}",
        "Content-Type": "application/json; charset=utf-8",
        "HTTP-Referer": "https://github.com/onthibacsinoitru9999-coder/hisx64-crossover",
        "X-Title": "HIS AI Integration Suite"
    }

    payload: Dict[str, Any] = {
        "model": model,
        "messages": messages,
        "max_tokens": max_tokens,
        "temperature": temperature
    }

    if response_format:
        payload["response_format"] = response_format

    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers=headers
    )

    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            raw_body = resp.read().decode("utf-8")
            return json.loads(raw_body)
    except urllib.error.HTTPError as he:
        err_msg = he.read().decode("utf-8", errors="ignore")
        raise RuntimeError(f"OpenRouter HTTP {he.code} ({he.reason}): {err_msg}")
    except Exception as ex:
        raise RuntimeError(f"Lỗi kết nối OpenRouter: {ex}")


def generate_with_fallback(
    messages: List[Dict[str, Any]],
    model_candidates: Optional[List[str]] = None,
    api_key: Optional[str] = None,
    json_mode: bool = False,
    max_tokens: int = 16384,
    verbose: bool = True
) -> Tuple[str, str]:
    """
    Thực hiện gọi API với cơ chế tự động chuyển tầng (Smart Multi-Tier Fallback).
    Trả về (content, model_thành_công).
    """
    if model_candidates is None:
        model_candidates = FREE_VISION_MODELS if any(
            isinstance(m.get("content"), list) for m in messages
        ) else FREE_REASONING_MODELS

    resp_format = {"type": "json_object"} if json_mode else None
    last_error = None

    for idx, model in enumerate(model_candidates, start=1):
        if verbose:
            print(f"   🤖 [Tier {idx}/{len(model_candidates)}] Đang gọi mô hình: {model} ...", flush=True)
        try:
            res = call_openrouter_raw(
                messages=messages,
                model=model,
                api_key=api_key,
                response_format=resp_format,
                max_tokens=max_tokens
            )
            choices = res.get("choices", [])
            if choices:
                content = choices[0].get("message", {}).get("content", "")
                if content:
                    if verbose:
                        cost = res.get("usage", {}).get("cost", 0)
                        print(f"   ✅ Thành công với [{model}] (Chi phí: ${cost})", flush=True)
                    return content, model
            last_error = f"Mô hình {model} trả về nội dung rỗng."
        except Exception as ex:
            last_error = str(ex)
            if verbose:
                print(f"   ⚠️  [{model}] gặp lỗi: {ex}. Chuyển sang mô hình tiếp theo...", flush=True)

    raise RuntimeError(f"Tất cả các mô hình trong danh sách đều thất bại! Lỗi cuối: {last_error}")


def extract_json_structured(
    prompt: str,
    image_path: Optional[str] = None,
    system_prompt: str = "",
    model_candidates: Optional[List[str]] = None
) -> Dict[str, Any]:
    """Trích xuất dữ liệu có cấu trúc JSON an toàn từ text hoặc ảnh"""
    user_content: Any
    if image_path:
        b64, mime = load_file_base64(image_path)
        user_content = [
            {"type": "text", "text": prompt},
            {
                "type": "image_url",
                "image_url": {"url": f"data:{mime};base64,{b64}"}
            }
        ]
        candidates = model_candidates or FREE_VISION_MODELS
    else:
        user_content = prompt
        candidates = model_candidates or FREE_CODE_MODELS

    messages = []
    if system_prompt:
        messages.append({"role": "system", "content": system_prompt})
    messages.append({"role": "user", "content": user_content})

    raw_text, used_model = generate_with_fallback(
        messages=messages,
        model_candidates=candidates,
        json_mode=True
    )

    # Làm sạch markdown JSON fence
    clean_text = re.sub(r"^```(?:json)?\s*", "", raw_text.strip(), flags=re.IGNORECASE)
    clean_text = re.sub(r"\s*```$", "", clean_text.strip())
    clean_text = clean_text.strip()

    return json.loads(clean_text)


def batch_parallel_generate(
    items: List[Dict[str, Any]],
    max_workers: int = 5,
    system_prompt: str = "Bạn là trợ lý AI y khoa chuyên khoa Chấn thương Chỉnh hình & Cột sống.",
    model_candidates: Optional[List[str]] = None
) -> List[Dict[str, Any]]:
    """
    Xử lý song song đồng thời nhiều bệnh nhân/yêu cầu (Concurrent Multi-Threading).
    Mỗi item trong items: {"id": str/int, "prompt": str, "image_path": Optional[str]}
    Trả về danh sách kết quả kèm thời gian xử lý.
    """
    import concurrent.futures
    import time

    results = []
    candidates = model_candidates or FREE_REASONING_MODELS

    def _process_one(item: Dict[str, Any]) -> Dict[str, Any]:
        item_id = item.get("id", "N/A")
        prompt = item.get("prompt", "")
        img = item.get("image_path", None)
        t_start = time.time()
        try:
            if img:
                res = extract_json_structured(prompt, image_path=img, system_prompt=system_prompt, model_candidates=FREE_VISION_MODELS)
                return {"id": item_id, "success": True, "data": res, "duration": round(time.time() - t_start, 2)}
            else:
                msgs = []
                if system_prompt:
                    msgs.append({"role": "system", "content": system_prompt})
                msgs.append({"role": "user", "content": prompt})
                txt, used_m = generate_with_fallback(msgs, model_candidates=candidates, verbose=False)
                return {"id": item_id, "success": True, "text": txt, "model": used_m, "duration": round(time.time() - t_start, 2)}
        except Exception as ex:
            return {"id": item_id, "success": False, "error": str(ex), "duration": round(time.time() - t_start, 2)}

    print(f"🚀 Bắt đầu xử lý song song {len(items)} tác vụ với {max_workers} worker threads...", flush=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = [executor.submit(_process_one, it) for it in items]
        for f in concurrent.futures.as_completed(futures):
            results.append(f.result())

    return results


if __name__ == "__main__":
    print("=== KIỂM TRA BỘ ĐIỀU PHỐI OPENROUTER MULTI-TIER & PARALLEL PIPELINE ===")
    k = get_openrouter_api_key()
    print(f"Khóa xác thực: {'Đã tìm thấy (' + k[:8] + '...)' if k else 'CHƯA CÓ'}")

    if k:
        print("\n1. Test câu hỏi ngắn:")
        msg = [{"role": "user", "content": "Xin chào! Trả lời 1 câu ngắn gọn."}]
        content, model = generate_with_fallback(msg, ["minimax/minimax-m3:free", "openrouter/free"])
        print(f"\nPhản hồi từ {model}:\n{content}\n")

