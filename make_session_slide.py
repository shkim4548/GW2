# -*- coding: utf-8 -*-
"""SESSION_SLIDE.pptx — 세션 생성 및 연결 초기화 한페이지 (v3 라이트 테마)"""
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.oxml.ns import qn
from pptx.oxml import parse_xml

W, H = Inches(13.33), Inches(7.5)

WHITE   = RGBColor(0xFF,0xFF,0xFF)
ACCENT  = RGBColor(0x2B,0x5B,0xA8)
DARK    = RGBColor(0x1A,0x1A,0x2E)
GRAY    = RGBColor(0x6B,0x72,0x80)
LGRAY   = RGBColor(0xD0,0xD5,0xDD)
BLU_BG  = RGBColor(0xEA,0xF3,0xFF); BLU_BD = RGBColor(0x44,0x72,0xC4)
GRN_BG  = RGBColor(0xEA,0xFF,0xF0); GRN_BD = RGBColor(0x70,0xAD,0x47)
ORG_BG  = RGBColor(0xFF,0xF0,0xE0); ORG_BD = RGBColor(0xE6,0x7E,0x22)
YEL_BG  = RGBColor(0xFF,0xFB,0xCC); YEL_BD = RGBColor(0xF0,0xC0,0x30)
T_HDR   = RGBColor(0xE8,0xEC,0xEF)
T_BLU   = RGBColor(0xDB,0xEA,0xFF)
T_GRN   = RGBColor(0xDB,0xFF,0xEC)
T_YEL   = RGBColor(0xFF,0xFB,0xCC)
T_ORG   = RGBColor(0xFF,0xED,0xD5)
T_RED   = RGBColor(0xFF,0xE8,0xE8)
PNT_BG  = RGBColor(0xEF,0xF4,0xFF)
PNT_BD  = RGBColor(0x44,0x72,0xC4)
PNT_LB  = RGBColor(0xE6,0x7E,0x22)
CODE_BG = RGBColor(0xF6,0xF8,0xFA)
KR = "맑은 고딕"; CO = "Consolas"

prs = Presentation()
prs.slide_width  = W
prs.slide_height = H
blank = prs.slide_layouts[6]

# ── 유틸 ──────────────────────────────────────────────────────────────────────
def rect(slide, x, y, w, h, fill=None, line=None, lw=Pt(1)):
    sh = slide.shapes.add_shape(1, x, y, w, h)
    sh.fill.background()
    if fill: sh.fill.solid(); sh.fill.fore_color.rgb = fill
    if line: sh.line.color.rgb = line; sh.line.width = lw
    else: sh.line.fill.background()
    return sh

def tb(slide, text, x, y, w, h, fn=KR, sz=Pt(11), bold=False, color=DARK,
       align=PP_ALIGN.LEFT, wrap=True):
    t = slide.shapes.add_textbox(x, y, w, h)
    tf = t.text_frame; tf.word_wrap = wrap
    p = tf.paragraphs[0]; p.alignment = align
    r = p.add_run(); r.text = text
    r.font.name = fn; r.font.size = sz
    r.font.bold = bold; r.font.color.rgb = color
    return t

def header(slide, title, subtitle=''):
    rect(slide, 0, 0, W, H, fill=WHITE)
    rect(slide, Inches(0.35), Inches(0.28), Inches(0.07), Inches(0.90), fill=ACCENT)
    tb(slide, title, Inches(0.52), Inches(0.22), Inches(12.3), Inches(0.55),
       sz=Pt(30), bold=True, color=DARK)
    if subtitle:
        tb(slide, subtitle, Inches(0.52), Inches(0.78), Inches(12.3), Inches(0.30),
           sz=Pt(12), color=GRAY)
    ln = slide.shapes.add_connector(1, Inches(0.35), Inches(1.15), Inches(12.98), Inches(1.15))
    ln.line.color.rgb = LGRAY; ln.line.width = Pt(0.75)

def point_box(slide, text, y=Inches(6.62)):
    rect(slide, Inches(0.35), y, Inches(12.63), Inches(0.65), fill=PNT_BG, line=PNT_BD)
    rect(slide, Inches(0.35), y, Inches(1.05),  Inches(0.65), fill=PNT_LB)
    tb(slide, 'POINT', Inches(0.37), y+Inches(0.13), Inches(1.0), Inches(0.40),
       sz=Pt(11), bold=True, color=WHITE, align=PP_ALIGN.CENTER)
    tb(slide, text, Inches(1.50), y+Inches(0.07), Inches(11.3), Inches(0.52),
       sz=Pt(10.5), color=DARK)

def step_box(slide, num, title_txt, body, x, y, w, h, bg, bd):
    rect(slide, x,             y, Inches(0.06), h, fill=bd)
    rect(slide, x+Inches(0.06),y, w-Inches(0.06), h, fill=bg, line=bd, lw=Pt(0.5))
    tb(slide, f'{num}  {title_txt}',
       x+Inches(0.14), y+Inches(0.05), w-Inches(0.20), Inches(0.26),
       sz=Pt(10.5), bold=True, color=bd)
    tb(slide, body,
       x+Inches(0.14), y+Inches(0.29), w-Inches(0.20), h-Inches(0.34),
       sz=Pt(9.5), color=DARK)

def cbox(slide, body, x, y, w, h, bg, bd, title='', tsz=Pt(10.5)):
    rect(slide, x, y, w, h, fill=bg, line=bd, lw=Pt(1.2))
    if title:
        tb(slide, title, x+Inches(0.1), y+Inches(0.06), w-Inches(0.2), Inches(0.28),
           sz=tsz, bold=True, color=bd)
        tb(slide, body,  x+Inches(0.1), y+Inches(0.32), w-Inches(0.2), h-Inches(0.38),
           sz=Pt(10), color=DARK)
    else:
        tb(slide, body, x+Inches(0.1), y+Inches(0.1), w-Inches(0.2), h-Inches(0.2),
           sz=Pt(10), color=DARK)

def table(slide, headers, rows, row_colors, x, y, col_widths, row_h=Inches(0.44)):
    nc = len(headers); nr = len(rows)+1
    tbl = slide.shapes.add_table(nr, nc, x, y, sum(col_widths), row_h*nr).table
    for ci, cw in enumerate(col_widths): tbl.columns[ci].width = cw

    def cfill(cell, color):
        tc = cell._tc; tcPr = tc.get_or_add_tcPr()
        sf = parse_xml(f'<a:solidFill xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">'
                       f'<a:srgbClr val="{str(color)}"/></a:solidFill>')
        for old in tcPr.findall(qn('a:solidFill')): tcPr.remove(old)
        tcPr.insert(0, sf)

    def ctext(cell, text, bold=False, sz=Pt(10), al=PP_ALIGN.LEFT):
        cell.text = ''
        tf = cell.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.alignment = al
        r = p.add_run(); r.text = text
        r.font.name = KR; r.font.size = sz
        r.font.bold = bold; r.font.color.rgb = DARK

    for ci, h in enumerate(headers):
        c = tbl.cell(0, ci); cfill(c, T_HDR)
        ctext(c, h, bold=True, sz=Pt(10.5), al=PP_ALIGN.CENTER)
    for ri, (row, rc) in enumerate(zip(rows, row_colors)):
        for ci, val in enumerate(row):
            c = tbl.cell(ri+1, ci); cfill(c, rc); ctext(c, val)
    for ri in range(nr): tbl.rows[ri].height = row_h
    return tbl

# ══════════════════════════════════════════════════════════════════════════════
#  슬라이드: 세션 생성 및 연결 초기화
# ══════════════════════════════════════════════════════════════════════════════
s = prs.slides.add_slide(blank)
header(s, '세션 생성 및 연결 초기화',
       'GW2_ServerCore  —  IOCP 기반 비동기 Accept → Session 활성화 → 패킷 수신 루프')

# ── 왼쪽: 연결 수립 4단계 ─────────────────────────────────────────────────────
LX = Inches(0.35)
LW = Inches(6.10)
SH = Inches(0.86)
GAP = Inches(0.06)
Y0  = Inches(1.28)

steps = [
    ('①', 'ServerService::Start()  —  Listener 초기화 & AcceptEx 등록',
     'Listener::StartAccept()  →  소켓 생성  →  IOCP 등록  →  Bind + Listen\n'
     'maxSessionCount 개수만큼 AcceptEvent 생성 + AcceptEx 비동기 선점 등록',
     BLU_BG, BLU_BD),

    ('②', 'ProcessAccept()  —  연결 수락 & 다음 대기 즉시 재등록',
     'IOCP 완료 통지  →  SetUpdateAcceptSocket()  →  getpeername()로 클라이언트 주소 획득\n'
     'session->ProcessConnect()  →  즉시 RegisterAccept() 재등록 (연속 Accept 대기 유지)',
     GRN_BG, GRN_BD),

    ('③', 'ProcessConnect()  —  세션 활성화 & 수신 루프 시작',
     '_connected.store(true)  →  Service::AddSession()  →  OnConnected() 가상함수 호출\n'
     'GameSession::OnConnected()  →  GSessionManager.Add()  →  RegisterRecv() 수신 루프 진입',
     YEL_BG, YEL_BD),

    ('④', 'PacketSession 수신 루프  —  스트림 → 패킷 경계 분리',
     'WSARecv 완료 → ProcessRecv() → RecvBuffer.OnWrite() → PacketSession::OnRecv() 파싱\n'
     '[size(2)][id(2)][data...] 헤더 검증 → OnRecvPacket() → HandlePacket() → RegisterRecv() 재등록',
     ORG_BG, ORG_BD),
]
for idx, (num, ttxt, body, bg, bd) in enumerate(steps):
    step_box(s, num, ttxt, body, LX, Y0 + idx*(SH+GAP), LW, SH, bg, bd)

# 클래스 계층 요약
HY = Y0 + 4*(SH+GAP)
cbox(s,
     'IocpObject  ←  Session (RecvBuffer 64KB · SendQueue)  ←  PacketSession (헤더 파싱 sealed)  ←  GameSession\n'
     'Service  ←  ServerService (Listener 보유)  /  ClientService (RegisterConnect 직접 호출)',
     LX, HY, LW, Inches(0.68), CODE_BG, LGRAY)

# ── 오른쪽 ────────────────────────────────────────────────────────────────────
RX = Inches(6.62)
RW = Inches(6.36)

# 주요 클래스 역할 표
tb(s, '주요 클래스 역할', RX, Inches(1.28), RW, Inches(0.26),
   sz=Pt(11), bold=True, color=DARK)

tbl_rows = [
    ['IocpCore',        'IOCP 핸들 생성·관리  Register() / Dispatch()\nWorker Thread 루프의 핵심'],
    ['Listener',        'AcceptEx 루프 유지  선점형 세션 소켓 할당\nmaxSessionCount 개수만큼 동시 수락 대기'],
    ['Session',         'RecvBuffer(64KB) · SendQueue 관리\n4종 IocpEvent (Connect/Recv/Send/Disconnect)'],
    ['PacketSession',   '[size|id|data] 스트림 → 패킷 분리 (OnRecv sealed)\nOnRecvPacket() 순수 가상 → GameSession 구현'],
    ['GameSession',     'OnConnected → GSessionManager 등록\n_currentPlayer(atomic) · Room weak_ptr 보유'],
]
table(s, ['클래스', '역할 및 책임'],
      tbl_rows, [T_BLU, T_GRN, T_YEL, T_ORG, T_RED],
      RX, Inches(1.58),
      [Inches(1.55), Inches(4.81)],
      row_h=Inches(0.52))

# 송신(Send) 메커니즘
cbox(s,
     'Send()  WRITE_LOCK  →  _sendQueue.push()  →  _sendRegistered.exchange(false) 이면 RegisterSend()\n'
     'RegisterSend()  →  큐 전체 drain  →  Scatter-Gather WSASend (여러 버퍼 1회 시스템 콜)\n'
     'ProcessSend()  →  큐 잔여 있으면 RegisterSend() 재호출 · 없으면 _sendRegistered=false',
     RX, Inches(4.42), RW, Inches(1.04),
     CODE_BG, BLU_BD, title='Send 메커니즘  —  Scatter-Gather + 중복 등록 방지')

# 연결 해제
cbox(s,
     'Disconnect()  _connected.exchange(false) 로 단 1회만 실행 보장  →  RegisterDisconnect()\n'
     'ProcessDisconnect()  →  OnDisconnected()  →  GSessionManager.Remove()  →  ReleaseSession()',
     RX, Inches(5.55), RW, Inches(0.80),
     YEL_BG, YEL_BD, title='연결 해제  —  Atomic 플래그로 중복 해제 방지')

point_box(s,
    'AcceptEx 선점 등록으로 Accept 완료 즉시 다음 대기 재등록 → 연결 공백 제거. '
    'SendQueue + Scatter-Gather로 중복 WSASend 방지. '
    'ProcessConnect() 안에서 OnConnected() → RegisterRecv() 순서를 보장해 수신 전 세션 등록 완결')

# ── 저장 ──────────────────────────────────────────────────────────────────────
OUT = r'D:\Dev\unity\GW2\SESSION_SLIDE.pptx'
prs.save(OUT)
print(f'저장 완료: {OUT}')
print(f'슬라이드 수: {len(prs.slides)}')
