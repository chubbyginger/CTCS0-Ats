import numpy as np

def compute_tilt(points, center=None):
    """
    根据仪表盘矩形在底图上的四个角点坐标，推算 Tilt 角度（angleX, angleY）。
    
    参数
    ----------
    points : list of tuple
        四个角点坐标，按顺序 (左上, 右上, 右下, 左下) 给出，每个坐标为 (x, y)。
        例如：[(1076,1538), (1462,1552), (1429,1873), (1038,1841)]
    center : tuple or None, optional
        相机光心坐标 (cx, cy)，通常为底图的中心点。
        若为 None，则自动取四个角点的几何中心作为近似。
    
    返回
    -------
    angleX_deg : float
        pitch 角度（绕 X 轴旋转），向上为正，单位：度。
    angleY_deg : float
        yaw 角度（绕 Y 轴旋转），向左为正，单位：度。
    """
    # 将点转换为 numpy 数组
    pts = np.array(points, dtype=float)
    if pts.shape != (4, 2):
        raise ValueError("points 必须包含 4 个 (x,y) 坐标")
    
    # 按顺序提取四个顶点
    tl, tr, br, bl = pts
    
    # ---------- 1. 计算水平消失点（上边与下边的交点）----------
    def line_intersection(p1, p2, p3, p4):
        """计算直线 p1p2 与 p3p4 的交点，若平行则返回 None"""
        A = np.array([p2 - p1, p4 - p3])
        b = p3 - p1
        try:
            t = np.linalg.solve(A, b)
        except np.linalg.LinAlgError:
            return None
        return p1 + t[0] * (p2 - p1)
    
    vp_horiz = line_intersection(tl, tr, bl, br)      # 上边与下边
    vp_vert  = line_intersection(tl, bl, tr, br)      # 左边与右边
    
    # ---------- 2. 确定光心 ----------
    if center is None:
        # 默认取四个点的中心
        cx = np.mean(pts[:, 0])
        cy = np.mean(pts[:, 1])
    else:
        cx, cy = center
    
    # ---------- 3. 计算焦距 f 及旋转角度 ----------
    # 若只有一个消失点或没有消失点（平面平行于像平面），角度为 0
    angleX = 0.0
    angleY = 0.0
    
    if vp_horiz is not None and vp_vert is not None:
        # 两个消失点都存在 -> 利用正交约束求焦距 f
        uh, vh = vp_horiz
        uv, vv = vp_vert
        du_h = uh - cx
        dv_h = vh - cy
        du_v = uv - cx
        dv_v = vv - cy
        
        # 正交消失点约束: du_h*du_v + dv_h*dv_v + f^2 = 0
        rhs = - (du_h * du_v + dv_h * dv_v)
        if rhs > 0:
            f = np.sqrt(rhs)
        else:
            # 数值不稳定时取绝对值，并给出警告
            f = np.sqrt(-rhs) if rhs < 0 else 1e-6
            print("警告: 消失点正交约束结果为负，已取绝对值，角度可能不准确。")
        
        # 计算角度（单位：弧度）
        # yaw: 水平消失点决定，向左为正（u 增加为正）
        angleY = np.arctan2(du_h, f)      # 注意 arctan2(delta_u, f)
        # pitch: 垂直消失点决定，向上为正（v 减小为正，即负的 dv_v）
        # 因为向上倾斜时垂直消失点位于光心上方（vv < cy），dv_v 为负，取负后为正
        angleX = -np.arctan2(dv_v, f)     # 负号使得向上倾斜时 angleX > 0
        
    elif vp_horiz is not None:
        # 只有水平消失点 -> 仅 yaw 非零
        du_h = vp_horiz[0] - cx
        # 无法确定 f，假设 f = 图像宽度（经验值），但这里设为 1 以使角度合理
        # 更严谨的做法是提醒用户输入焦距
        print("警告: 垂直方向无消失点，无法计算 pitch，假定 angleX=0")
        angleY = np.arctan2(du_h, 1.0)
    elif vp_vert is not None:
        print("警告: 水平方向无消失点，无法计算 yaw，假定 angleY=0")
        dv_v = vp_vert[1] - cy
        angleX = -np.arctan2(dv_v, 1.0)
    
    # 转换为度
    angleX_deg = np.degrees(angleX)
    angleY_deg = np.degrees(angleY)
    
    return angleX_deg, angleY_deg


# ========== 使用示例 ==========
if __name__ == "__main__":
    # 用户提供的四个角点（左上、右上、右下、左下）
    points = [(1076, 1538), (1462, 1552), (1429, 1873), (1038, 1841)]
    
    # 可选：指定光心坐标（若未知则留空自动计算）
    # 底图尺寸未知时，自动取四点中心作为光心
    angleX, angleY = compute_tilt(points)
    
    print(f"推算出的 Tilt 角度：")
    print(f"angleX (pitch, 向上为正) = {angleX:.2f}°")
    print(f"angleY (yaw,   向左为正) = {angleY:.2f}°")