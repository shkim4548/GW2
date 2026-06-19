from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.util import Inches, Pt
import copy

# ── 컬러 팔레트 ──────────────────────────────────────────────
NAVY      = RGBColor(0x1E, 0x27, 0x61)   # 딥 네이비 (다크 슬라이드 BG / 강조)
TEAL      = RGBColor(0x00, 0xC9, 0xA7)   # 민트 틸  (포인트)
WHITE     = RGBColor(0xFF, 0xFF, 0xFF)
LIGHT_BG  = RGBColor(0xF5, 0xF7, 0xFA)  # 라이트 슬라이드 BG
CARD_BG   = RGBColor(0xFF, 0xFF, 0xFF)
BLUE      = RGBColor(0x3A, 0xA0, 0xFF)   # 밝은 파랑
SUBTEXT   = RGBColor(0x5A, 0x64, 0x78)   # 서브 텍스트
DARK_TEXT = RGBColor(0x1E, 0x27, 0x61)   # 라이트 슬라이드 본문
MID_BG    = RGBColor(0xE8, 0xEE, 0xF9)  # 카드/박스 배경
ORANGE    = RGBColor(0xFF, 0x8C, 0x00)

W = Inches(10)
H = Inches(5.625)

# ── 헬퍼 ───────────────────────────────────────────────────────
def add_rect(slide, x, y, w, h, fill=None, line_color=None, line_w=None):
    from pptx.enum.shapes import MSO_SHAPE_TYPE
    shape = slide.shapes.add_shape(1, Inches(x), Inches(y), Inches(w), Inches(h))
    shape.line.fill.background()
    if fill:
        shape.fill.solid()
        shape.fill.fore_color.rgb = fill
    else:
        shape.fill.background()
    if line_color:
        shape.line.color.rgb = line_color
        if line_w:
            shape.line.width = Pt(line_w)
    else:
        shape.line.fill.background()
    return shape

def add_text(slide, text, x, y, w, h,
             size=16, bold=False, italic=False, color=None,
             align=PP_ALIGN.LEFT, valign=None, wrap=True, font="Calibri"):
    txBox = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    txBox.word_wrap = wrap
    tf = txBox.text_frame
    tf.word_wrap = wrap
    p = tf.paragraphs[0]
    p.alignment = align
    run = p.add_run()
    run.text = text
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.italic = italic
    run.font.name = font
    if color:
        run.font.color.rgb = color
    return txBox

def add_textbox_lines(slide, lines, x, y, w, h,
                      size=13, bold=False, color=None,
                      align=PP_ALIGN.LEFT, font="Calibri", line_space=None):
    """lines: list of (text, bold, color, size) or just strings"""
    txBox = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    txBox.word_wrap = True
    tf = txBox.text_frame
    tf.word_wrap = True
    first = True
    for item in lines:
        if isinstance(item, str):
            txt, b, c, s = item, bold, color, size
        else:
            txt = item.get("text", "")
            b   = item.get("bold", bold)
            c   = item.get("color", color)
            s   = item.get("size", size)
        if first:
            p = tf.paragraphs[0]
            first = False
        else:
            p = tf.add_paragraph()
        p.alignment = align
        if line_space:
            from pptx.util import Pt as UPt
            from pptx.oxml.ns import qn
            from lxml import etree
            pPr = p._p.get_or_add_pPr()
            spcAft = etree.SubElement(pPr, qn('a:spcAft'))
            spcPts = etree.SubElement(spcAft, qn('a:spcPts'))
            spcPts.set('val', str(int(line_space * 100)))
        run = p.add_run()
        run.text = txt
        run.font.size = Pt(s)
        run.font.bold = b
        run.font.name = font
        if c:
            run.font.color.rgb = c
    return txBox

def set_bg(slide, color):
    from pptx.oxml.ns import qn
    from lxml import etree
    bg = slide.background
    fill = bg.fill
    fill.solid()
    fill.fore_color.rgb = color

def title_bar(slide, title_text, bg_color=None):
    """상단 타이틀 바 (다크 슬라이드에선 생략, 라이트 슬라이드용)"""
    add_rect(slide, 0, 0, 10, 0.7, fill=bg_color or NAVY)
    add_text(slide, title_text, 0.3, 0.08, 9.4, 0.55,
             size=22, bold=True, color=WHITE, align=PP_ALIGN.LEFT)

def section_label(slide, text, x, y, w=2.5, h=0.3, bg=TEAL):
    add_rect(slide, x, y, w, h, fill=bg)
    add_text(slide, text, x + 0.08, y + 0.02, w - 0.1, h - 0.04,
             size=10, bold=True, color=WHITE, align=PP_ALIGN.LEFT)

def card(slide, x, y, w, h, bg=CARD_BG, accent=None):
    add_rect(slide, x, y, w, h, fill=bg,
             line_color=RGBColor(0xD0, 0xD8, 0xEC), line_w=0.5)
    if accent:
        add_rect(slide, x, y, 0.07, h, fill=accent)

# ═══════════════════════════════════════════════════════════════
# SLIDE 1 — 타이틀
# ═══════════════════════════════════════════════════════════════
prs = Presentation()
prs.slide_width  = W
prs.slide_height = H

sl = prs.slides.add_slide(prs.slide_layouts[6])  # blank
set_bg(sl, NAVY)

# 좌측 틸 세로 바
add_rect(sl, 0, 0, 0.12, 5.625, fill=TEAL)

# 메인 타이틀
add_text(sl, "GW2 Game Server", 0.4, 1.1, 9.2, 1.4,
         size=52, bold=True, color=WHITE, align=PP_ALIGN.LEFT, font="Calibri")
add_text(sl, "C++ 멀티플레이 게임 서버 포트폴리오", 0.4, 2.55, 9.2, 0.65,
         size=22, bold=False, color=TEAL, align=PP_ALIGN.LEFT)

# 구분선
add_rect(sl, 0.4, 3.3, 7.0, 0.04, fill=RGBColor(0x3A, 0xA0, 0xFF))

# 기술 스택 태그
tags = ["C++ IOCP", "Protobuf", "A* 경로탐색", "Unity Client", "Server-Auth"]
tx = 0.4
for tag in tags:
    add_rect(sl, tx, 3.55, 1.55, 0.38,
             fill=RGBColor(0x2A, 0x37, 0x7A),
             line_color=BLUE, line_w=0.8)
    add_text(sl, tag, tx + 0.08, 3.57, 1.4, 0.35,
             size=11, bold=True, color=BLUE, align=PP_ALIGN.CENTER)
    tx += 1.65

# 하단 서브텍스트
add_text(sl, "RTS / MOBA  |  서버 권위 구조  |  JobQueue 스레드 안전 설계",
         0.4, 4.85, 9.2, 0.45,
         size=12, color=RGBColor(0x9A, 0xA5, 0xC4), align=PP_ALIGN.LEFT)

# ═══════════════════════════════════════════════════════════════
# SLIDE 2 — 프로젝트 개요
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "프로젝트 개요")

# 왼쪽 설명 카드
card(sl, 0.3, 0.9, 5.5, 2.3, accent=NAVY)
add_text(sl, "프로젝트 소개", 0.55, 0.95, 5.0, 0.38,
         size=13, bold=True, color=NAVY)
add_textbox_lines(sl, [
    {"text": "• RTS/MOBA 장르의 C++ 멀티플레이 게임 서버", "size": 12},
    {"text": "• 서버 권위(Server-Authoritative) 구조", "size": 12},
    {"text": "• 모든 게임 로직은 서버에서 계산·검증", "size": 12},
    {"text": "• 클라이언트는 Unity로 결과 표현만 담당", "size": 12},
    {"text": "• Protobuf 직렬화 + IOCP 비동기 네트워크", "size": 12},
], 0.55, 1.35, 5.0, 1.7, color=DARK_TEXT)

# 오른쪽 — 기술 스택
card(sl, 6.0, 0.9, 3.7, 2.3, accent=TEAL)
add_text(sl, "기술 스택", 6.25, 0.95, 3.3, 0.38,
         size=13, bold=True, color=NAVY)

stack = [
    ("서버",   "C++17 · IOCP · Protobuf"),
    ("AI",     "A* · WalkableGrid · 상태머신"),
    ("네트워크","JobQueue · DoAsync 패턴"),
    ("클라이언트","Unity · NavMesh · C#"),
]
sy = 1.38
for label, val in stack:
    add_text(sl, label, 6.25, sy, 1.1, 0.3, size=10, bold=True, color=TEAL)
    add_text(sl, val,   7.4,  sy, 2.2, 0.3, size=10, color=DARK_TEXT)
    sy += 0.42

# 아래 — 구성 다이어그램
card(sl, 0.3, 3.35, 9.4, 1.95, accent=BLUE)
add_text(sl, "전체 구성", 0.55, 3.4, 3.0, 0.35,
         size=13, bold=True, color=NAVY)

boxes = [
    ("Unity Client",    BLUE,  "클라이언트\n(C# / NavMesh)"),
    ("←  Protobuf  →",  RGBColor(0xCC,0xCC,0xCC), ""),
    ("GW2_ServerCore",  NAVY,  "IOCP 세션\n패킷 수신·송신"),
    ("→  DoAsync  →",   RGBColor(0xCC,0xCC,0xCC), ""),
    ("Lobby / Room",    TEAL,  "게임 로직\n30Hz 루프"),
]
bx = 0.5
for title_b, col, sub in boxes:
    if "→" in title_b or "←" in title_b:
        add_text(sl, title_b, bx, 3.95, 1.0, 0.5,
                 size=10, color=SUBTEXT, align=PP_ALIGN.CENTER)
        bx += 1.05
        continue
    add_rect(sl, bx, 3.8, 1.7, 1.1, fill=col,
             line_color=RGBColor(0xB0,0xBA,0xD0), line_w=0.5)
    add_text(sl, title_b, bx + 0.05, 3.83, 1.6, 0.38,
             size=10, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    if sub:
        add_text(sl, sub, bx + 0.05, 4.2, 1.6, 0.6,
                 size=9, color=WHITE, align=PP_ALIGN.CENTER)
    bx += 2.75

# ═══════════════════════════════════════════════════════════════
# SLIDE 3 — 서버 아키텍처
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "서버 아키텍처")

# 레이어 다이어그램 (좌측)
layers = [
    (NAVY,  "GW2_ServerCore",     "IOCP · GameSession · 패킷 송수신"),
    (RGBColor(0x1A,0x5F,0x9E), "Lobby (GLobby 싱글톤)", "NavMesh 로드 · Room 관리 · Stats 로드"),
    (RGBColor(0x1D,0x82,0xB0), "Room  (JobQueue 상속)", "게임 루프 · Handler 함수 · Broadcast"),
    (TEAL,  "Object 계층",        "Player · Minion · Turret · Nexus · Baron"),
]
ly = 0.85
for color, name, desc in layers:
    add_rect(sl, 0.3, ly, 5.6, 0.82, fill=color)
    add_text(sl, name, 0.4, ly + 0.06, 3.2, 0.38,
             size=13, bold=True, color=WHITE)
    add_text(sl, desc, 0.4, ly + 0.44, 5.3, 0.35,
             size=10, color=RGBColor(0xCC,0xDD,0xFF))
    if ly < 3.9:
        add_rect(sl, 2.7, ly + 0.84, 0.4, 0.18, fill=RGBColor(0x88,0x99,0xBB))
        add_text(sl, "▼", 2.75, ly + 0.83, 0.35, 0.2,
                 size=10, color=SUBTEXT, align=PP_ALIGN.CENTER)
    ly += 1.02

# 우측 — 핵심 설계 원칙 카드
card(sl, 6.1, 0.85, 3.6, 4.4, accent=TEAL)
add_text(sl, "핵심 설계 원칙", 6.35, 0.92, 3.1, 0.35,
         size=13, bold=True, color=NAVY)

principles = [
    ("스레드 안전", "Room JobQueue로 직렬 실행\n모든 Room 로직 단일 스레드"),
    ("DoAsync 패턴", "크로스 스레드 호출 시\n반드시 DoAsync 사용"),
    ("30 Hz 게임 루프", "Lobby가 33ms Tick으로\n모든 Room UpdateRoom 호출"),
    ("서버 권위", "이동·전투·버프 모두 서버\n클라이언트는 표현만 담당"),
]
py = 1.38
for title_p, desc in principles:
    add_rect(sl, 6.2, py, 3.35, 0.88,
             fill=MID_BG,
             line_color=RGBColor(0xC5,0xCF,0xE8), line_w=0.5)
    add_rect(sl, 6.2, py, 0.06, 0.88, fill=BLUE)
    add_text(sl, title_p, 6.35, py + 0.04, 3.05, 0.3,
             size=11, bold=True, color=NAVY)
    add_text(sl, desc, 6.35, py + 0.36, 3.05, 0.48,
             size=9, color=SUBTEXT)
    py += 1.0

# ═══════════════════════════════════════════════════════════════
# SLIDE 4 — 스레드 모델 & JobQueue
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "스레드 모델 & JobQueue")

# 3개 스레드 박스
threads = [
    (NAVY, "IOCP Worker Thread",
     ["• 네트워크 I/O 처리",
      "• 패킷 수신 → 디스패치",
      "• GameSession 관리",
      "• 비동기 Send/Recv"]),
    (RGBColor(0x1A,0x5F,0x9E), "Lobby Main Thread",
     ["• RunRooms() — 30Hz Tick",
      "• LobbyUpdate(deltaTime)",
      "• room→DoAsync 디스패치",
      "• Room 생성·삭제 관리"]),
    (TEAL, "Room JobQueue",
     ["• 직렬 실행 보장",
      "• HandleSkill / HandleMove",
      "• UpdateRoom(dt)",
      "• Broadcast 처리"]),
]
tx = 0.25
for color, name, items in threads:
    add_rect(sl, tx, 0.85, 3.0, 3.0, fill=color)
    add_text(sl, name, tx + 0.12, 0.9, 2.8, 0.45,
             size=13, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    add_rect(sl, tx + 0.12, 1.38, 2.76, 0.04,
             fill=RGBColor(0xFF,0xFF,0xFF))
    iy = 1.52
    for item in items:
        add_text(sl, item, tx + 0.18, iy, 2.65, 0.38,
                 size=10.5, color=WHITE)
        iy += 0.45
    tx += 3.25

# DoAsync 화살표 설명
add_rect(sl, 0.25, 4.05, 9.5, 0.04, fill=RGBColor(0xCC,0xDD,0xFF))
add_text(sl, "DoAsync 패턴 — 크로스 스레드 안전 호출", 0.25, 4.15, 9.5, 0.32,
         size=13, bold=True, color=NAVY, align=PP_ALIGN.CENTER)

# 코드 박스
add_rect(sl, 0.3, 4.55, 9.4, 0.9,
         fill=RGBColor(0x1A,0x1A,0x2E),
         line_color=BLUE, line_w=0.8)
add_textbox_lines(sl, [
    {"text": "// ✅ 안전: Minion 스레드에서 Room 함수 호출", "size": 10, "color": RGBColor(0x7E,0xC8,0xA0)},
    {"text": "room->DoAsync(&Room::HandleMinionAttack, self, currentTarget);", "size": 10.5, "color": RGBColor(0xAA,0xD4,0xFF), "bold": True},
    {"text": "// ❌ 위험: 직접 호출 — 절대 금지  room->HandleMinionAttack(self, currentTarget);", "size": 10, "color": RGBColor(0xFF,0x7A,0x7A)},
], 0.5, 4.6, 9.0, 0.82, font="Consolas")

# ═══════════════════════════════════════════════════════════════
# SLIDE 5 — 이동 시스템
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "이동 시스템 — 서버 권위 + 클라이언트 예측")

# 좌측: 서버 처리 흐름
card(sl, 0.3, 0.85, 5.5, 4.45, accent=NAVY)
add_text(sl, "서버 이동 처리 흐름", 0.55, 0.92, 5.0, 0.35,
         size=13, bold=True, color=NAVY)

flow_steps = [
    (NAVY,  "① C_MOVE 수신",      "출발·목적지 좌표 전달"),
    (RGBColor(0x1A,0x5F,0x9E), "② ValidateMove()", "walkable 여부 검증"),
    (RGBColor(0x1D,0x82,0xB0), "③ A* FindPath()", "WalkableGrid 경로탐색"),
    (RGBColor(0x1A,0x9E,0x8A), "④ SmoothPath()", "불필요 꺾임 제거"),
    (TEAL,  "⑤ S_MOVE 브로드캐스트", "100ms(10Hz) 주기 위치 동기화"),
    (BLUE,  "⑥ S_MOVE_END",       "경로 완주 시 최종 위치 확정"),
]
fy = 1.35
for color, name, desc in flow_steps:
    add_rect(sl, 0.45, fy, 3.4, 0.56, fill=color)
    add_text(sl, name, 0.55, fy + 0.06, 3.2, 0.26,
             size=11, bold=True, color=WHITE)
    add_text(sl, desc, 0.55, fy + 0.3, 3.2, 0.24,
             size=9, color=RGBColor(0xCC,0xEE,0xFF))
    if fy < 4.55:
        add_text(sl, "↓", 1.8, fy + 0.57, 0.4, 0.2,
                 size=12, color=SUBTEXT, align=PP_ALIGN.CENTER)
    fy += 0.72

# 우측: 클라이언트 예측
card(sl, 6.0, 0.85, 3.7, 2.1, accent=BLUE)
add_text(sl, "클라이언트 예측 (MyPlayer)", 6.25, 0.92, 3.3, 0.35,
         size=12, bold=True, color=NAVY)
add_textbox_lines(sl, [
    {"text": "• C_MOVE 전송과 동시에 NavMesh로 경로 예측"},
    {"text": "• NavMeshAgent.updatePosition = false"},
    {"text": "• transform.position 수동 제어"},
    {"text": "• S_MOVE_END 수신 시 3m 초과 차이만 보정"},
], 6.25, 1.35, 3.35, 1.45, size=10, color=DARK_TEXT)

# 텔레포트 처리 카드
card(sl, 6.0, 3.1, 3.7, 2.2, accent=TEAL)
add_text(sl, "텔레포트 처리", 6.25, 3.17, 3.3, 0.35,
         size=12, bold=True, color=NAVY)
add_textbox_lines(sl, [
    {"text": "Mobility 카드 조건 (서버):"},
    {"text": "  buffType==0 && damage==0 && heal==0", "size": 9, "color": SUBTEXT},
    {"text": "→ 즉시 SetPosVector() + S_MOVE_END"},
    {"text": ""},
    {"text": "클라이언트: diff > 3.0m → agent.Warp()"},
    {"text": "NavMesh 위에 올바르게 착지 보장", "size": 9, "color": SUBTEXT},
], 6.25, 3.55, 3.35, 1.6, size=10, color=DARK_TEXT)

# ═══════════════════════════════════════════════════════════════
# SLIDE 6 — 미니언 AI 상태머신
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "미니언 AI — 상태머신")

# 상태 박스들
states = [
    (0.3,  2.1, NAVY,  "IDLE",             "주변 적 탐색\n레인 잔여 확인"),
    (2.6,  0.95, RGBColor(0x1A,0x5F,0x9E), "LINE_TRACE", "레인 경로탐색\nA*(laneId 필터)"),
    (4.9,  2.1, RGBColor(0x1D,0x82,0xB0),  "CHASE",      "타겟 추적\n재경로 0.2s"),
    (7.2,  2.1, TEAL,  "ATTACK",           "공격 쿨다운\n피해 처리"),
]
for sx, sy, color, name, desc in states:
    add_rect(sl, sx, sy, 2.1, 1.25, fill=color,
             line_color=RGBColor(0xAA,0xBB,0xDD), line_w=0.5)
    add_text(sl, name, sx + 0.05, sy + 0.1, 2.0, 0.42,
             size=14, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    add_text(sl, desc, sx + 0.1, sy + 0.55, 1.9, 0.6,
             size=10, color=RGBColor(0xCC,0xEE,0xFF), align=PP_ALIGN.CENTER)

# 전이 화살표 텍스트 — 각 상태 박스(IDLE/LINE_TRACE/CHASE/ATTACK)와 겹치지 않는
# 빈 공간(코너·박스 사이 간극)에만 배치. (x, y, w, h, text)
arrows = [
    (2.45, 2.45, 1.40, 0.55, "적 없음\n+ wp 있음 →"),   # IDLE 우측 빈 공간
    (5.30, 0.95, 1.60, 0.32, "타겟 사망 →"),             # LINE_TRACE·CHASE 위 빈 공간
    (5.30, 1.30, 1.60, 0.32, "↑ 적 탐지"),
    (5.30, 1.65, 2.60, 0.32, "사거리 진입 →  /  ← 이탈"),
    (4.05, 2.75, 0.80, 0.32, "leash 이탈 ↓"),           # CHASE 시작 전(4.9)까지로 폭 축소
]
for ax, ay, aw, ah, txt in arrows:
    add_text(sl, txt, ax, ay, aw, ah,
             size=9, color=SUBTEXT, align=PP_ALIGN.CENTER)

# 하단 설명 박스들
cards_info = [
    ("_pathPending 가드",
     "DoAsync 중복 요청 방지\nLINE_TRACE: RAII 자동 해제\nCHASE: 수동 ClearPathPending()"),
    ("레인 필터 (laneId)",
     "allowedLaneId=0: 전체 허용\nallowedLaneId=1: 상단 레인\nallowedLaneId=3: 하단 레인"),
    ("타겟 우선순위",
     "Turret (3) > Minion (2)\n> Player/Nexus (1)\n아군 = -1 (제외)"),
    ("leash 거리",
     "detectionRange × 1.5 이탈\n→ LINE_TRACE 복귀\n1.5초 재탐지 유예"),
]
cx = 0.3
for title_c, desc in cards_info:
    add_rect(sl, cx, 3.65, 2.3, 1.65, fill=CARD_BG,
             line_color=RGBColor(0xC5,0xCF,0xE8), line_w=0.5)
    add_rect(sl, cx, 3.65, 0.06, 1.65, fill=TEAL)
    add_text(sl, title_c, cx + 0.15, 3.7, 2.1, 0.32,
             size=10, bold=True, color=NAVY)
    add_text(sl, desc, cx + 0.15, 4.05, 2.1, 1.18,
             size=9, color=SUBTEXT)
    cx += 2.42

# ═══════════════════════════════════════════════════════════════
# SLIDE 7 — 카드/스킬 시스템
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "카드/스킬 시스템")

# 좌측: 카드 분류
card(sl, 0.3, 0.85, 4.1, 4.45, accent=NAVY)
add_text(sl, "카드 분류 (target_type)", 0.55, 0.92, 3.7, 0.35,
         size=12, bold=True, color=NAVY)

types = [
    (NAVY,  "Self (0)",        "즉시 자신에게 적용\n버프·회복 카드"),
    (RGBColor(0x1A,0x5F,0x9E), "ClickPoint (1)", "클릭 지점으로 이동\n텔레포트·AOE 카드"),
    (TEAL,  "ClickTarget (2)", "좌클릭 적 대상\n단일 공격·스턴 카드"),
]
ty = 1.35
for color, name, desc in types:
    add_rect(sl, 0.45, ty, 3.8, 1.1, fill=color)
    add_text(sl, name, 0.58, ty + 0.1, 3.5, 0.38,
             size=12, bold=True, color=WHITE)
    add_text(sl, desc, 0.58, ty + 0.5, 3.5, 0.52,
             size=10, color=RGBColor(0xCC,0xEE,0xFF))
    ty += 1.25

# 우측: HandleSkill 흐름
card(sl, 4.6, 0.85, 5.1, 4.45, accent=BLUE)
add_text(sl, "HandleSkill() 처리 흐름", 4.85, 0.92, 4.7, 0.35,
         size=12, bold=True, color=NAVY)

flow = [
    (NAVY,   "C_SKILL 수신"),
    (None,   "target_id == 0 ?"),
    (RGBColor(0x1A,0x5F,0x9E), "카드 소유·사용 처리"),
    (RGBColor(0x1A,0x5F,0x9E), "Heal 처리 → S_HP_CHANGE"),
    (RGBColor(0x1A,0x5F,0x9E), "버프 처리 → S_BUFF_APPLIED"),
    (RGBColor(0x1A,0x5F,0x9E), "S_SKILL 브로드캐스트"),
    (TEAL,   "Mobility? → 즉시 이동"),
    (None,   "─────── or ───────"),
    (RGBColor(0x8B,0x5E,0x1A), "타겟 검증 (팀·사망·거리)"),
    (RGBColor(0xB0,0x4A,0x00), "데미지 계산 + 방어 감소"),
    (ORANGE, "S_HP_CHANGE + S_DIE"),
]
fy = 1.32
for color, text in flow:
    if color is None:
        add_text(sl, text, 4.75, fy, 4.85, 0.22,
                 size=10, color=SUBTEXT, align=PP_ALIGN.CENTER)
        fy += 0.26
        continue
    add_rect(sl, 4.75, fy, 4.85, 0.30, fill=color)
    add_text(sl, text, 4.85, fy + 0.04, 4.65, 0.22,
             size=10, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    fy += 0.34

# ═══════════════════════════════════════════════════════════════
# SLIDE 8 — 버프 시스템
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "버프 시스템")

# 4개 BuffType 카드
buffs = [
    (NAVY,  "BuffAttack (1)",
     "서버: _attackMult = value\n(예: 1.5 = 150%)",
     "Player.cpp:71\n_attackBuffTimer 타이머 관리"),
    (RGBColor(0x1A,0x5F,0x9E), "BuffDefense (2)",
     "서버: _defenseReduct = value\n(예: 0.3 = 30% 피해 감소)",
     "Room.cpp:488\ndamage × (1 - _defenseReduct)"),
    (RGBColor(0x1D,0x82,0xB0), "BuffSpeed (3)",
     "서버: _speedMult = value\n(예: 2.0 = 이동속도 2배)",
     "Player.cpp:119\nremaindist × _speedMult"),
    (TEAL,  "BuffAttackSpeed (4)",
     "서버: _attackSpeedMult = value\n(예: 0.5 = 쿨타임 절반)",
     "Room.cpp:380\ninterval / _attackSpeedMult"),
]
bx = 0.3
for color, name, effect, detail in buffs:
    add_rect(sl, bx, 0.85, 2.25, 2.35, fill=color,
             line_color=RGBColor(0xAA,0xBB,0xDD), line_w=0.5)
    add_text(sl, name, bx + 0.1, 0.9, 2.08, 0.44,
             size=12, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    add_rect(sl, bx + 0.08, 1.37, 2.1, 0.02, fill=RGBColor(0xFF,0xFF,0xFF))
    add_text(sl, effect, bx + 0.1, 1.45, 2.05, 0.75,
             size=9.5, color=RGBColor(0xCC,0xEE,0xFF))
    add_text(sl, detail, bx + 0.1, 2.22, 2.05, 0.85,
             size=8.5, color=RGBColor(0x99,0xBB,0xDD), italic=True)
    bx += 2.38

# 하단 — 서버→클라이언트 흐름
card(sl, 0.3, 3.35, 9.4, 1.95, accent=TEAL)
add_text(sl, "서버 → 클라이언트 버프 흐름", 0.55, 3.42, 4.0, 0.32,
         size=12, bold=True, color=NAVY)

flow_items = [
    "카드 사용\n(HandleSkill)",
    "→",
    "Player 버프\n타이머 적용",
    "→",
    "S_BUFF_APPLIED\n(대상에게만)",
    "→",
    "BaseController\n.ApplyBuff()",
    "→",
    "UI_StatusBox\n표시 갱신",
]
fx = 0.5
for item in flow_items:
    if item == "→":
        add_text(sl, "→", fx, 3.9, 0.45, 0.4,
                 size=16, bold=True, color=SUBTEXT, align=PP_ALIGN.CENTER)
        fx += 0.45
        continue
    add_rect(sl, fx, 3.78, 1.44, 1.22, fill=MID_BG,
             line_color=RGBColor(0xC5,0xCF,0xE8), line_w=0.5)
    add_text(sl, item, fx + 0.08, 3.9, 1.29, 1.0,
             size=9.5, color=DARK_TEXT, align=PP_ALIGN.CENTER)
    fx += 1.49

# BuffDefense 특이사항
add_text(sl, "※ BuffDefense value: 0.3 = 30% 감소 비율.  UI 표시 시 ×100 변환 필요  (_shield = value × 100f)",
         0.45, 5.3, 9.1, 0.25, size=9, color=SUBTEXT, italic=True)

# ═══════════════════════════════════════════════════════════════
# SLIDE 9 — 게임 루프 & 세션 생명주기
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, LIGHT_BG)
title_bar(sl, "게임 루프 & 세션 생명주기")

# 좌측: 생명주기 흐름
card(sl, 0.3, 0.85, 3.8, 4.45, accent=NAVY)
add_text(sl, "세션 생명주기", 0.55, 0.92, 3.4, 0.35,
         size=12, bold=True, color=NAVY)

lifecycle = [
    (NAVY,  "로그인"),
    (None,  "↓"),
    (RGBColor(0x1A,0x5F,0x9E), "로비 입장"),
    (None,  "↓"),
    (RGBColor(0x1D,0x82,0xB0), "캐릭터 선택"),
    (None,  "↓"),
    (RGBColor(0x1A,0x9E,0x8A), "게임 룸 매칭"),
    (None,  "↓"),
    (TEAL,  "게임 시작 (포탑·넥서스 스폰)"),
    (None,  "↓"),
    (BLUE,  "게임 루프 [30Hz]"),
    (None,  "↓"),
    (ORANGE,"사망 / 리스폰 (5초)"),
    (None,  "↓"),
    (RGBColor(0xAA,0x22,0x22), "넥서스 파괴 → 게임 종료"),
]
ly = 1.35
for color, text in lifecycle:
    if color is None:
        add_text(sl, text, 1.7, ly, 0.4, 0.13,
                 size=9, color=SUBTEXT, align=PP_ALIGN.CENTER)
        ly += 0.16
        continue
    add_rect(sl, 0.5, ly, 3.4, 0.30, fill=color)
    add_text(sl, text, 0.6, ly + 0.04, 3.2, 0.22,
             size=9, bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    ly += 0.33

# 우측 위: 게임 루프 UpdateRoom
card(sl, 4.3, 0.85, 5.4, 2.3, accent=TEAL)
add_text(sl, "UpdateRoom(deltaTime) — 30Hz", 4.55, 0.92, 5.0, 0.35,
         size=12, bold=True, color=NAVY)
add_textbox_lines(sl, [
    {"text": "① 각 Object::UpdateController(dt)"},
    {"text": "   Player: 경로이동 + 버프 타이머 감소", "size": 10, "color": SUBTEXT},
    {"text": "   Minion: 상태머신 (IDLE→ATTACK)", "size": 10, "color": SUBTEXT},
    {"text": "② S_MOVE 브로드캐스트 (100ms 주기)"},
    {"text": "③ 미니언 웨이브 스폰 (30초 간격)"},
    {"text": "④ 리스폰 타이머 감소"},
    {"text": "⑤ 골드 자동 지급 (1초, +20 Gold)"},
], 4.55, 1.35, 5.0, 1.65, size=11, color=DARK_TEXT)

# 우측 아래: 타이밍 표
card(sl, 4.3, 3.3, 5.4, 2.0, accent=BLUE)
add_text(sl, "타이밍 요약", 4.55, 3.37, 5.0, 0.35,
         size=12, bold=True, color=NAVY)

timings = [
    ("게임 틱",           "33ms  (30Hz)"),
    ("S_MOVE 브로드캐스트", "100ms (10Hz)"),
    ("미니언 웨이브",      "30초 간격"),
    ("골드 자동 지급",     "1초 / +20 Gold"),
    ("플레이어 리스폰",    "사망 후 5초"),
    ("미니언 타겟 탐색",   "0.1초 간격"),
]
header_added = False
row_y = 3.82
for label, val in timings:
    bg = MID_BG if timings.index((label,val)) % 2 == 0 else CARD_BG
    add_rect(sl, 4.35, row_y, 5.3, 0.3, fill=bg)
    add_text(sl, label, 4.5, row_y + 0.05, 2.8, 0.24, size=9.5, color=DARK_TEXT)
    add_text(sl, val,   7.3, row_y + 0.05, 2.2, 0.24, size=9.5, bold=True,
             color=NAVY, align=PP_ALIGN.RIGHT)
    row_y += 0.3

# ═══════════════════════════════════════════════════════════════
# SLIDE 10 — 마무리
# ═══════════════════════════════════════════════════════════════
sl = prs.slides.add_slide(prs.slide_layouts[6])
set_bg(sl, NAVY)

add_rect(sl, 0, 0, 0.12, 5.625, fill=TEAL)

add_text(sl, "구현을 통해 배운 것들", 0.4, 0.5, 9.2, 0.75,
         size=32, bold=True, color=WHITE, align=PP_ALIGN.LEFT)
add_rect(sl, 0.4, 1.3, 7.0, 0.04, fill=BLUE)

achievements = [
    ("서버 권위 아키텍처",
     "모든 게임 로직을 서버에서 처리하는 구조 설계.\n클라이언트 예측과 서버 보정을 동시에 구현."),
    ("JobQueue 기반 스레드 안전 설계",
     "DoAsync 패턴으로 크로스 스레드 호출을 직렬화.\nRace condition 없이 멀티스레드 환경 운용."),
    ("A* 경로탐색 & 미니언 AI",
     "WalkableGrid + laneId 필터로 레인 기반 탐색 구현.\n상태머신으로 IDLE→CHASE→ATTACK 전환 관리."),
    ("Protobuf 기반 패킷 설계",
     "클라이언트-서버 간 타입 안전한 메시지 직렬화.\n패킷 방향(C_/S_) 규칙으로 흐름 명확화."),
]

ax = 0.4
ay = 1.5
for i, (title_a, desc) in enumerate(achievements):
    bw, bh = 4.55, 1.75
    bx = ax + (i % 2) * 4.75
    by = ay + (i // 2) * 1.9
    add_rect(sl, bx, by, bw, bh,
             fill=RGBColor(0x2A, 0x37, 0x7A),
             line_color=BLUE, line_w=0.8)
    add_rect(sl, bx, by, 0.07, bh, fill=TEAL)
    add_text(sl, title_a, bx + 0.2, by + 0.1, bw - 0.3, 0.38,
             size=13, bold=True, color=TEAL)
    add_text(sl, desc, bx + 0.2, by + 0.52, bw - 0.35, 1.1,
             size=10.5, color=RGBColor(0xCC, 0xDD, 0xFF))

add_text(sl, "D:\\Dev\\unity\\GW2  |  C++ Server + Unity Client",
         0.4, 5.25, 9.2, 0.3,
         size=10, color=RGBColor(0x7A,0x85,0xAA), align=PP_ALIGN.RIGHT)

# ── 저장 ─────────────────────────────────────────────────────
output = r"D:\Dev\unity\GW2\GW2_Portfolio.pptx"
prs.save(output)
print(f"Saved: {output}")
