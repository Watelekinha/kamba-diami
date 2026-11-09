export interface Province {
  id: number;
  name: string;
}

export interface Municipality {
  id: number;
  name: string;
  provinceId: number;
}

export interface Neighborhood {
  id: number;
  name: string;
  municipalityId: number;
}

export interface ServiceCategory {
  id: number;
  name: string;
}
