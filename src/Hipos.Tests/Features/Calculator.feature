@Calculator @Smoke
Feature: Calculadora de Windows
  Como usuario
  Quiero usar la Calculadora de Windows
  Para realizar operaciones básicas

  Scenario Outline: Operación básica
    Given la Calculadora está abierta
    When pulso "<operando1>"
    And pulso "<operador>"
    And pulso "<operando2>"
    And pulso "="
    Then el resultado es "<resultado>"

    Examples:
      | operando1 | operador | operando2 | resultado |
      | 5         | +        | 3         | 8         |
      | 9         | -        | 2         | 7         |
      | 4         | *        | 3         | 12        |
      | 8         | /        | 2         | 4         |
