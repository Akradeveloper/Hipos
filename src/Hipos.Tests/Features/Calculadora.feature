# language: es
@Calculadora @Demo
Característica: Calculadora de Windows
  Como usuario
  Quiero usar la Calculadora de Windows
  Para realizar operaciones matemáticas

  @Smoke
  Escenario: Verificar que la Calculadora se abra correctamente
    Dado que la calculadora está abierta
    Cuando verifico el título de la ventana
    Entonces el título debería contener "Calculadora" o "Calculator"

  Escenario: Verificar que la ventana de la Calculadora sea visible y accesible
    Dado que la calculadora está abierta
    Cuando verifico la visibilidad de la ventana
    Entonces la ventana debería estar visible y habilitada

  Escenario: Verificar que la interfaz de la Calculadora tenga elementos interactivos
    Dado que la calculadora está abierta
    Cuando verifico los elementos de la interfaz
    Entonces debería haber elementos de UI disponibles

  Escenario: Mostrar información sobre la ventana de la Calculadora
    Dado que la calculadora está abierta
    Cuando obtengo la información de la ventana
    Entonces debería mostrar el título, clase, process ID y dimensiones

  @Complex
  Escenario: Realizar una suma simple
    Dado que la calculadora está abierta
    Cuando limpio la calculadora
    Y realizo la operación "2 + 3"
    Entonces el resultado debería ser "5"

  @Complex
  Escenario: Realizar una resta
    Dado que la calculadora está abierta
    Cuando limpio la calculadora
    Y realizo la operación "10 - 4"
    Entonces el resultado debería ser "6"

  @Complex
  Escenario: Realizar una multiplicación
    Dado que la calculadora está abierta
    Cuando limpio la calculadora
    Y realizo la operación "7 * 8"
    Entonces el resultado debería ser "56"

  @Complex
  Escenario: Realizar una división
    Dado que la calculadora está abierta
    Cuando limpio la calculadora
    Y realizo la operación "20 / 4"
    Entonces el resultado debería ser "5"

  @Complex
  Escenario: Realizar operaciones secuenciales
    Dado que la calculadora está abierta
    Cuando limpio la calculadora
    Y ingreso el número "5"
    Y presiono el botón de suma
    Y ingreso el número "3"
    Y presiono el botón igual
    Entonces el resultado intermedio debería ser "8"
    Cuando presiono el botón de multiplicación
    Y ingreso el número "2"
    Y presiono el botón igual
    Entonces el resultado final debería ser "16"

  @Complex
  Escenario: Verificar que todos los botones numéricos estén disponibles
    Dado que la calculadora está abierta
    Cuando verifico la disponibilidad de los botones numéricos del 0 al 9
    Entonces todos los botones numéricos deberían estar disponibles

  @Complex
  Escenario: Verificar que el botón Clear limpia correctamente el display
    Dado que la calculadora está abierta
    Cuando limpio la calculadora
    Y ingreso los números "123"
    Y verifico el valor del display
    Entonces el display debería contener "123"
    Cuando presiono el botón Clear
    Y verifico el valor del display
    Entonces el display debería mostrar "0"
